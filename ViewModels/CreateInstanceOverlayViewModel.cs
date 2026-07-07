using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NexusLauncher.Models;
using NexusLauncher.Services;
using NexusLauncher.ViewModels.Base;
using System;
using System.Collections.ObjectModel;
using System.Linq;

namespace NexusLauncher.ViewModels;

public partial class CreateInstanceOverlayViewModel : ViewModelBase
{
    private readonly VersionService _versionService = new();
    private readonly InstanceService _instanceService;

    [ObservableProperty] private string instanceName = "";
    [ObservableProperty] private string nameErrorText = "";

    [ObservableProperty]
    private string selectedVersion = "";
    partial void OnSelectedVersionChanged(string value) => UpdateAvailableLoaders();

    [ObservableProperty] private string selectedLoader = "vanilla";
    [ObservableProperty] private bool visible;

    public ObservableCollection<string> AvailableVersions { get; } = new();
    public ObservableCollection<string> AvailableLoaders { get; } = new();

    public string TitleLabel => LanguageService.Instance.T("dialog.create.title");
    public string SubtitleLabel => LanguageService.Instance.T("dialog.create.subtitle");
    public string NameLabel => LanguageService.Instance.T("dialog.create.name_label");
    public string NamePlaceholder => LanguageService.Instance.T("dialog.create.name_placeholder");
    public string VersionLabelField => LanguageService.Instance.T("dialog.create.version_label");
    public string LoaderLabel => LanguageService.Instance.T("dialog.create.loader_label");
    public string CancelLabel => LanguageService.Instance.T("dialog.create.button.cancel");
    public string CreateLabel => LanguageService.Instance.T("dialog.create.button.create");

    public IRelayCommand CreateCommand { get; }
    public IRelayCommand CancelCommand { get; }
    public event Action? Closed;

    public CreateInstanceOverlayViewModel(InstanceService instanceService)
    {
        _instanceService = instanceService;
        CreateCommand = new RelayCommand(CreateAction);
        CancelCommand = new RelayCommand(CancelAction);

        LanguageService.Instance.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(LanguageService.Instance.Current))
            {
                OnPropertyChanged(nameof(TitleLabel));
                OnPropertyChanged(nameof(SubtitleLabel));
                OnPropertyChanged(nameof(NameLabel));
                OnPropertyChanged(nameof(NamePlaceholder));
                OnPropertyChanged(nameof(VersionLabelField));
                OnPropertyChanged(nameof(LoaderLabel));
                OnPropertyChanged(nameof(CancelLabel));
                OnPropertyChanged(nameof(CreateLabel));
            }
        };

        _ = InitializeAsync();
    }

    public void Initialize()
    {
        Visible = true;
        InstanceName = "";
        NameErrorText = "";
        if (AvailableVersions.Count > 0) SelectedVersion = AvailableVersions[0];
        SelectedLoader = "vanilla";
    }

    private async System.Threading.Tasks.Task InitializeAsync()
    {
        var versions = await _versionService.GetOfficialVersionsAsync();
        AvailableVersions.Clear();
        foreach (var v in versions) AvailableVersions.Add(v.Id);
        SelectedVersion = AvailableVersions.FirstOrDefault() ?? "";
        UpdateAvailableLoaders();
    }

    private void UpdateAvailableLoaders()
    {
        if (string.IsNullOrWhiteSpace(SelectedVersion)) return;

        var loaders = _versionService.GetCompatibleLoaders(SelectedVersion);
        AvailableLoaders.Clear();
        foreach (var l in loaders) AvailableLoaders.Add(l);

        if (!AvailableLoaders.Contains(SelectedLoader)) SelectedLoader = "vanilla";
    }

    private void CreateAction()
    {
        if (string.IsNullOrWhiteSpace(InstanceName))
        {
            NameErrorText = "O nome da instância é obrigatório";
            return;
        }
        NameErrorText = "";
        if (string.IsNullOrWhiteSpace(SelectedVersion)) return;
        _instanceService.Create(InstanceName, SelectedVersion, SelectedLoader, "latest");
        Visible = false;
        Closed?.Invoke();
    }

    private void CancelAction()
    {
        Visible = false;
        Closed?.Invoke();
    }
}