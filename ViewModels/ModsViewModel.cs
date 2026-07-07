using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NexusLauncher.Models;
using NexusLauncher.Services;
using NexusLauncher.ViewModels.Base;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace NexusLauncher.ViewModels;

public partial class ModsViewModel : ViewModelBase
{
    private const int PageSize = 30;

    private readonly ModrinthService _modrinthService;
    private readonly CurseForgeService _curseForgeService;
    private readonly ModpackInstallerService _installerService;
    private readonly SelectInstanceOverlayViewModel _selectOverlay;
    private readonly DownloadService _downloadService;
    private readonly SettingsService _settingsService;

    private int _currentOffset;
    private int _totalCount;
    private ProjectItem? _pendingInstall;

    [ObservableProperty] private string searchText = string.Empty;
    partial void OnSearchTextChanged(string value) => DebounceSearch();

    private CancellationTokenSource? _debounceCts;
    private void DebounceSearch()
    {
        _debounceCts?.Cancel();
        _debounceCts = new CancellationTokenSource();
        var token = _debounceCts.Token;
        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(400, token);
                if (!token.IsCancellationRequested)
                    await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(SearchAsync);
            }
            catch (OperationCanceledException) { }
        });
    }

    [ObservableProperty] private string selectedSource = "Modrinth";
    [ObservableProperty] private string selectedProjectType = "modpack";
    partial void OnSelectedProjectTypeChanged(string value) => DebounceSearch();
    [ObservableProperty] private string selectedLoader = string.Empty;
    partial void OnSelectedLoaderChanged(string value) => DebounceSearch();
    [ObservableProperty] private string minecraftVersion = string.Empty;
    partial void OnMinecraftVersionChanged(string value) => DebounceSearch();
    [ObservableProperty] private string selectedSort = "relevance";
    partial void OnSelectedSortChanged(string value) => DebounceSearch();
    [ObservableProperty] private string statusText = "...";
    [ObservableProperty] private bool isBusy;
    [ObservableProperty] private bool isLoadingMore;
    [ObservableProperty] private bool hasResults = true;
    [ObservableProperty] private bool hasMoreResults;

    partial void OnSelectedSourceChanged(string value) => _ = SearchAsync();

    public ObservableCollection<string> Sources { get; } = new() { "All", "Modrinth", "CurseForge" };
    public ObservableCollection<string> ProjectTypes { get; } = new() { "modpack", "mod", "resourcepack", "shader" };
    public ObservableCollection<string> Loaders { get; } = new() { string.Empty, "fabric", "forge", "neoforge", "quilt" };
    public ObservableCollection<string> Sorts { get; } = new() { "relevance", "downloads", "follows", "updated", "newest" };

    public ObservableCollection<ProjectItem> Projects { get; } = new();

    public string TitleLabel => LanguageService.Instance.T("mods.title");
    public string SubtitleLabel => LanguageService.Instance.T("mods.subtitle");
    public string SearchPlaceholder => LanguageService.Instance.T("mods.search.placeholder");
    public string VersionPlaceholder => LanguageService.Instance.T("mods.version.placeholder");
    public string SearchButtonLabel => LanguageService.Instance.T("mods.button.search");
    public string InstallButtonLabel => LanguageService.Instance.T("mods.button.install");
    public string LoadingStatus => LanguageService.Instance.T("mods.status.loading");
    public string LoadingMoreLabel => LanguageService.Instance.T("mods.status.loading_more");

    public ModsViewModel(
        ModrinthService modrinthService,
        CurseForgeService curseForgeService,
        SelectInstanceOverlayViewModel selectOverlay)
    {
        _modrinthService = modrinthService;
        _curseForgeService = curseForgeService;
        _selectOverlay = selectOverlay;
        _downloadService = new DownloadService();
        _settingsService = new SettingsService();
        _installerService = new ModpackInstallerService(
            modrinthService,
            curseForgeService,
            _downloadService,
            _settingsService,
            new InstanceService(_settingsService));

        _selectOverlay.Confirmed += () => _ = OnSelectOverlayConfirmedAsync();

        LanguageService.Instance.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(LanguageService.Instance.Current))
            {
                OnPropertyChanged(nameof(TitleLabel));
                OnPropertyChanged(nameof(SubtitleLabel));
                OnPropertyChanged(nameof(SearchPlaceholder));
                OnPropertyChanged(nameof(VersionPlaceholder));
                OnPropertyChanged(nameof(SearchButtonLabel));
                OnPropertyChanged(nameof(InstallButtonLabel));
                OnPropertyChanged(nameof(LoadingStatus));
                OnPropertyChanged(nameof(LoadingMoreLabel));
            }
        };

        _ = SearchAsync();
    }

    [RelayCommand]
    private async Task SearchAsync()
    {
        _currentOffset = 0;
        await FetchResultsAsync(false);
    }

    [RelayCommand]
    private async Task LoadMoreAsync()
    {
        if (!HasMoreResults || IsLoadingMore) return;
        _currentOffset += PageSize;
        await FetchResultsAsync(true);
    }

    private async Task FetchResultsAsync(bool append)
    {
        _searchCts?.Cancel();
        _searchCts = new CancellationTokenSource();
        var ct = _searchCts.Token;

        if (append)
            IsLoadingMore = true;
        else
            IsBusy = true;

        StatusText = append ? LoadingMoreLabel : LoadingStatus;

        try
        {
            var options = new SearchOptions
            {
                Query = SearchText,
                ProjectType = SelectedProjectType,
                Loader = SelectedLoader,
                MinecraftVersion = MinecraftVersion,
                Sort = SelectedSort,
                Offset = _currentOffset
            };

            if (!append)
            {
                Projects.Clear();
                HasResults = false;
            }

            var prevCount = Projects.Count;

            if (SelectedSource == "All")
            {
                SearchResult cfResult;
                SearchResult mrResult;

                try
                {
                    mrResult = await _modrinthService.SearchAsync(options, ct);
                }
                catch
                {
                    mrResult = new SearchResult();
                }

                try
                {
                    cfResult = await _curseForgeService.SearchAsync(options, ct);
                }
                catch
                {
                    cfResult = new SearchResult();
                }

                ct.ThrowIfCancellationRequested();

                _totalCount = cfResult.TotalCount + mrResult.TotalCount;

                var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (var p in Projects)
                    seen.Add(p.Title);

                foreach (var project in mrResult.Results)
                {
                    if (seen.Add(project.Title))
                        Projects.Add(project);
                }
                foreach (var project in cfResult.Results)
                {
                    if (seen.Add(project.Title))
                        Projects.Add(project);
                }

                HasMoreResults = (_currentOffset + PageSize < cfResult.TotalCount)
                              || (_currentOffset + PageSize < mrResult.TotalCount);
            }
            else
            {
                SearchResult result;

                if (SelectedSource == "CurseForge")
                    result = await _curseForgeService.SearchAsync(options, ct);
                else
                    result = await _modrinthService.SearchAsync(options, ct);

                ct.ThrowIfCancellationRequested();

                _totalCount = result.TotalCount;

                foreach (var project in result.Results) Projects.Add(project);
                HasMoreResults = _currentOffset + PageSize < _totalCount;
            }

            HasResults = Projects.Count > 0;
            StatusText = $"{Projects.Count} / {_totalCount} resultado(s)";

            for (int i = prevCount; i < Projects.Count; i++)
                _ = Projects[i].LoadIconAsync();
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            StatusText = $"Erro: {ex.Message}";
            Console.WriteLine(ex);
        }
        finally
        {
            IsBusy = false;
            IsLoadingMore = false;
        }
    }

    private CancellationTokenSource? _searchCts;

    [RelayCommand]
    private void Install(ProjectItem project)
    {
        if (project is null) return;

        if (project.ProjectType?.Equals("modpack", StringComparison.OrdinalIgnoreCase) == true)
        {
            _ = InstallModpackAsync(project);
            return;
        }

        _pendingInstall = project;
        _selectOverlay.Initialize(project);
    }

    private async Task InstallModpackAsync(ProjectItem project)
    {
        project.InstallState = InstallState.Installing;
        project.InstallProgress = 0;
        project.InstallProgressText = "Preparando...";

        try
        {
            IsBusy = true;
            string instanceName = project.Title.Replace(" ", "_");
            var progress = new Progress<DownloadProgressInfo>(p =>
            {
                project.InstallProgress = p.Percent;
                project.InstallProgressText = p.State == "Baixando" ? $"{p.CurrentFile} ({p.Percent:F0}%)" : p.State;
            });
            await _installerService.InstallModpackAsync(project, instanceName, progress);
            project.InstallState = InstallState.Installed;
            project.InstallProgressText = "Instalado";
            StatusText = $"{project.Title} ok!";
        }
        catch (Exception ex)
        {
            project.InstallState = InstallState.Error;
            project.InstallProgressText = ex.Message;
            StatusText = $"Erro: {ex.Message}";
            Console.WriteLine(ex);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task OnSelectOverlayConfirmedAsync()
    {
        var project = _pendingInstall;
        var instance = _selectOverlay.SelectedInstance;
        if (project is null || instance is null) return;

        project.InstallState = InstallState.Installing;
        project.InstallProgress = 0;
        project.InstallProgressText = "Obtendo arquivo...";

        try
        {
            IsBusy = true;

            string downloadUrl;
            string fileName;

            if (project.Source == "CurseForge")
            {
                var cfFile = await _curseForgeService.GetLatestFileAsync(project.CurseForgeId);
                if (cfFile is null)
                {
                    project.InstallState = InstallState.Error;
                    project.InstallProgressText = "Arquivo nao encontrado";
                    StatusText = "Erro: mod sem arquivos disponiveis no CurseForge";
                    return;
                }

                if (!string.IsNullOrWhiteSpace(cfFile.DownloadUrl))
                {
                    downloadUrl = cfFile.DownloadUrl;
                }
                else
                {
                    var redirectUrl = await _curseForgeService.GetDownloadUrlAsync(project.CurseForgeId, cfFile.Id);
                    if (redirectUrl is null)
                    {
                        project.InstallState = InstallState.Error;
                        project.InstallProgressText = "URL de download nao disponivel";
                        StatusText = "Erro: URL de download nao disponivel no CurseForge";
                        return;
                    }
                    downloadUrl = redirectUrl;
                }
                fileName = cfFile.FileName;
            }
            else
            {
                var versions = await _modrinthService.GetVersionsAsync(project.ProjectId);
                var version = versions.Find(v => v.Files?.Exists(f => f.Primary) == true)
                          ?? versions.Find(v => v.Files?.Count > 0);

                if (version is null && versions.Count > 0)
                    version = versions[0];

                if (version?.Files is null || version.Files.Count == 0)
                {
                    project.InstallState = InstallState.Error;
                    project.InstallProgressText = "Versao nao encontrada";
                    StatusText = "Erro: versão não encontrada no Modrinth";
                    return;
                }

                var file = version.Files.Find(f => f.Primary) ?? version.Files[0];
                downloadUrl = file.Url;
                fileName = file.Filename;
            }

            var targetDir = project.ProjectType switch
            {
                "shader" => "shaderpacks",
                "resourcepack" => "resourcepacks",
                _ => "mods"
            };

            var instancePath = instance.Path;
            var targetFolder = Path.Combine(instancePath, targetDir);
            Directory.CreateDirectory(targetFolder);
            var targetPath = Path.Combine(targetFolder, fileName);

            project.InstallProgressText = $"Baixando {fileName}...";

            var progress = new Progress<DownloadProgressInfo>(p =>
            {
                project.InstallProgress = p.Percent;
                project.InstallProgressText = $"{fileName} ({p.Percent:F0}%)";
                DownloadManagerService.Instance.ReportGlobalProgress(fileName, p.Percent, "Baixando");
            });

            await _downloadService.DownloadFileAsync(downloadUrl, targetPath, progress);

            project.InstallState = InstallState.Installed;
            project.InstallProgressText = "Instalado";
            StatusText = $"{project.Title} instalado em {instance.Name}!";
            DownloadManagerService.Instance.ClearGlobalProgress();
        }
        catch (Exception ex)
        {
            project.InstallState = InstallState.Error;
            project.InstallProgressText = ex.Message;
            StatusText = $"Erro: {ex.Message}";
            Console.WriteLine(ex);
            DownloadManagerService.Instance.ClearGlobalProgress();
        }
        finally
        {
            IsBusy = false;
        }
    }
}
