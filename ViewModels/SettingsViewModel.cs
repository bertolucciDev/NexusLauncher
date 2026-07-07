using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using NexusLauncher.Minecraft;
using CommunityToolkit.Mvvm.Input;
using NexusLauncher.Models;
using NexusLauncher.Services;
using NexusLauncher.ViewModels.Base;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace NexusLauncher.ViewModels;

public partial class CategoryFilterItem : ObservableObject
{
    [ObservableProperty]
    private bool isSelected;

    public string DisplayName { get; }
    public string Value { get; }

    public CategoryFilterItem(string value, string displayName, bool isSelected = true)
    {
        Value = value;
        DisplayName = displayName;
        IsSelected = isSelected;
    }
}

public partial class SettingsViewModel : ViewModelBase
{
    private readonly JavaService _javaService = new();
    private readonly MinecraftService _minecraftService = new();
    private readonly SettingsService _settingsService = new();

    [ObservableProperty] private string javaPath = "Não detectado";
    [ObservableProperty] private string minecraftDirectory = string.Empty;
    [ObservableProperty] private double allocatedRamGb;
    [ObservableProperty] private int maxRamGb;
    [ObservableProperty] private bool closeLauncherOnGameStart;
    [ObservableProperty] private bool reopenLauncherAfterGameClose;
    [ObservableProperty] private bool minimizeLauncherOnGameStart;
    [ObservableProperty] private bool keepLauncherInBackground = true;
    [ObservableProperty] private bool fullscreen;
    [ObservableProperty] private string selectedResolution = "1280x720";

    [ObservableProperty]
    private string selectedTheme = "Dark";
    partial void OnSelectedThemeChanged(string value)
    {
        ThemeService.Instance.Current = value == "Light" ? AppTheme.Light : AppTheme.Dark;
        ThemeService.Instance.Apply();
    }

    [ObservableProperty]
    private string selectedLanguage = "pt-BR";
    partial void OnSelectedLanguageChanged(string value)
    {
        LanguageService.Instance.ApplyByCode(value);
    }

    [ObservableProperty] private string curseForgeApiKey = "";
    [ObservableProperty] private string jvmArgs = "";
    [ObservableProperty] private bool downloadJavaAutomatically;
    [ObservableProperty] private bool autoUpdateLauncher;
    [ObservableProperty] private string saveStatus = "";
    [ObservableProperty] private bool saveStatusIsSuccess = true;
    partial void OnSaveStatusChanged(string value)
    {
        OnPropertyChanged(nameof(HasSaveStatus));
    }
    public bool HasSaveStatus => !string.IsNullOrEmpty(SaveStatus);

    public ObservableCollection<string> Resolutions { get; } = new() { "1280x720", "1600x900", "1920x1080", "2560x1440" };
    public ObservableCollection<string> Themes { get; } = new() { "Dark", "Light" };
    public ObservableCollection<string> Languages { get; } = new() { "pt-BR", "en", "es" };
    public ObservableCollection<CategoryFilterItem> CategoryFilters { get; } = new();

    public string TitleLabel => LanguageService.Instance.T("settings.title");
    public string SubtitleLabel => LanguageService.Instance.T("settings.subtitle");
    public string SectionPerformance => LanguageService.Instance.T("settings.section.performance");
    public string SectionJava => LanguageService.Instance.T("settings.section.java");
    public string SectionDirectories => LanguageService.Instance.T("settings.section.directories");
    public string SectionWindow => LanguageService.Instance.T("settings.section.window");
    public string SectionBehavior => LanguageService.Instance.T("settings.section.behavior");
    public string SectionAppearance => LanguageService.Instance.T("settings.section.appearance");
    public string SaveButtonLabel => LanguageService.Instance.T("settings.button.save");
    public string RestoreButtonLabel => LanguageService.Instance.T("settings.button.restore");
    public string DetectButtonLabel => LanguageService.Instance.T("settings.button.detect");
    public string OpenFolderButtonLabel => LanguageService.Instance.T("settings.button.open_folder");
    public string SectionVersions => LanguageService.Instance.T("settings.section.versions");
    public string SubsectionCategoryFilters => LanguageService.Instance.T("settings.subsection.category_filters");
    public string StatusSaved => LanguageService.Instance.T("settings.status.saved");
    public string StatusLoaded => LanguageService.Instance.T("settings.status.loaded");
    public string StatusRestored => LanguageService.Instance.T("settings.status.restored");
    public string StatusJavaDetected => LanguageService.Instance.T("settings.status.java_detected");
    public string StatusJavaNotDetected => LanguageService.Instance.T("settings.status.java_not_detected");

    public string AllocatedRamText => $"RAM: {(int)AllocatedRamGb} GB / {MaxRamGb} GB";

    public IRelayCommand SaveCommand { get; }
    public IRelayCommand RestoreDefaultsCommand { get; }
    public IRelayCommand OpenMinecraftFolderCommand { get; }
    public IRelayCommand DetectJavaCommand { get; }
    public IAsyncRelayCommand SelectJavaCommand { get; }

    public SettingsViewModel()
    {
        SaveCommand = new RelayCommand(Save);
        RestoreDefaultsCommand = new RelayCommand(RestoreDefaults);
        OpenMinecraftFolderCommand = new RelayCommand(OpenMinecraftFolder);
        DetectJavaCommand = new RelayCommand(DetectJava);
        SelectJavaCommand = new AsyncRelayCommand(SelectJavaAsync);

        LanguageService.Instance.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(LanguageService.Instance.Current))
            {
                OnPropertyChanged(nameof(TitleLabel));
                OnPropertyChanged(nameof(SubtitleLabel));
                OnPropertyChanged(nameof(SectionPerformance));
                OnPropertyChanged(nameof(SectionJava));
                OnPropertyChanged(nameof(SectionDirectories));
                OnPropertyChanged(nameof(SectionWindow));
                OnPropertyChanged(nameof(SectionBehavior));
                OnPropertyChanged(nameof(SectionAppearance));
                OnPropertyChanged(nameof(SaveButtonLabel));
                OnPropertyChanged(nameof(RestoreButtonLabel));
                OnPropertyChanged(nameof(DetectButtonLabel));
                OnPropertyChanged(nameof(OpenFolderButtonLabel));
                OnPropertyChanged(nameof(SectionVersions));
                OnPropertyChanged(nameof(SubsectionCategoryFilters));
                OnPropertyChanged(nameof(StatusSaved));
                OnPropertyChanged(nameof(StatusLoaded));
                OnPropertyChanged(nameof(StatusRestored));
                OnPropertyChanged(nameof(StatusJavaDetected));
                OnPropertyChanged(nameof(StatusJavaNotDetected));
            }
        };

        Load();
    }

    private void Load()
    {
        var settings = _settingsService.Load();
        MaxRamGb = _settingsService.GetMaxRamGb();
        AllocatedRamGb = settings.AllocatedRamGb;
        CloseLauncherOnGameStart = settings.CloseLauncherOnGameStart;
        ReopenLauncherAfterGameClose = settings.ReopenLauncherAfterGameClose;
        MinimizeLauncherOnGameStart = settings.MinimizeLauncherOnGameStart;
        KeepLauncherInBackground = settings.KeepLauncherInBackground;
        Fullscreen = settings.Fullscreen;
        SelectedResolution = settings.Resolution;
        SelectedTheme = settings.Theme;
        SelectedLanguage = settings.Language;
        JavaPath = string.IsNullOrWhiteSpace(settings.JavaPath) ? _javaService.FindJavaPath() ?? "Não detectado" : settings.JavaPath;
        MinecraftDirectory = settings.MinecraftDirectory;
        CurseForgeApiKey = settings.CurseForgeApiKey;
        JvmArgs = settings.JvmArgs;
        DownloadJavaAutomatically = settings.DownloadJavaAutomatically;
        AutoUpdateLauncher = settings.AutoUpdateLauncher;
        SaveStatus = StatusLoaded;

        LoadCategoryFilters(settings);
        LanguageService.Instance.ApplyByCode(SelectedLanguage);
    }

    private static readonly (string Value, string Label)[] CategoryDefinitions =
    {
        ("Release", "Release"),
        ("Snapshot", "Snapshot"),
        ("Forge", "Forge"),
        ("NeoForge", "NeoForge"),
        ("Fabric", "Fabric"),
        ("Quilt", "Quilt"),
        ("OptiFine", "OptiFine"),
        ("LiteLoader", "LiteLoader"),
        ("Modpack", "Modpack"),
        ("Custom", "Custom"),
    };

    private void LoadCategoryFilters(LauncherSettings settings)
    {
        CategoryFilters.Clear();
        var enabled = settings.EnabledCategories;
        var hasFilter = enabled.Count > 0;
        foreach (var (value, label) in CategoryDefinitions)
        {
            var item = new CategoryFilterItem(value, label, !hasFilter || enabled.Contains(value));
            item.PropertyChanged += OnCategoryFilterChanged;
            CategoryFilters.Add(item);
        }
    }

    private void OnCategoryFilterChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(CategoryFilterItem.IsSelected)) return;
        var settings = _settingsService.Load();
        settings.EnabledCategories = CategoryFilters.Where(f => f.IsSelected).Select(f => f.Value).ToList();
        _settingsService.Save(settings);
    }

    partial void OnAllocatedRamGbChanged(double value)
    {
        OnPropertyChanged(nameof(AllocatedRamText));
    }

    private void Save()
    {
        var settings = _settingsService.Load();
        settings.AllocatedRamGb = (int)AllocatedRamGb;
        settings.CloseLauncherOnGameStart = CloseLauncherOnGameStart;
        settings.ReopenLauncherAfterGameClose = ReopenLauncherAfterGameClose;
        settings.MinimizeLauncherOnGameStart = MinimizeLauncherOnGameStart;
        settings.KeepLauncherInBackground = KeepLauncherInBackground;
        settings.Fullscreen = Fullscreen;
        settings.Resolution = SelectedResolution;
        settings.Theme = SelectedTheme;
        settings.Language = SelectedLanguage;
        settings.MinecraftDirectory = MinecraftDirectory;
        settings.JavaPath = JavaPath == "Não detectado" ? string.Empty : JavaPath;
        settings.CurseForgeApiKey = CurseForgeApiKey;
        settings.JvmArgs = JvmArgs;
        settings.DownloadJavaAutomatically = DownloadJavaAutomatically;
        settings.AutoUpdateLauncher = AutoUpdateLauncher;
        settings.EnabledCategories = CategoryFilters.Where(f => f.IsSelected).Select(f => f.Value).ToList();
        _settingsService.Save(settings);
        SaveStatus = StatusSaved;
        SaveStatusIsSuccess = true;
    }

    private void RestoreDefaults()
    {
        MaxRamGb = _settingsService.GetMaxRamGb();
        AllocatedRamGb = Math.Min(4, MaxRamGb);
        CloseLauncherOnGameStart = false;
        ReopenLauncherAfterGameClose = true;
        MinimizeLauncherOnGameStart = true;
        KeepLauncherInBackground = true;
        Fullscreen = false;
        SelectedResolution = "1280x720";
        SelectedTheme = "Dark";
        SelectedLanguage = "pt-BR";
        DownloadJavaAutomatically = false;
        AutoUpdateLauncher = true;
        JvmArgs = "";
        CurseForgeApiKey = string.Empty;
        MinecraftDirectory = string.Empty;
        JavaPath = _javaService.FindJavaPath() ?? "Não detectado";
        foreach (var f in CategoryFilters) f.IsSelected = true;
        SaveStatus = StatusRestored;
        SaveStatusIsSuccess = true;
    }

    private void OpenMinecraftFolder()
    {
        try
        {
            if (!Directory.Exists(MinecraftDirectory)) Directory.CreateDirectory(MinecraftDirectory);
            var psi = new System.Diagnostics.ProcessStartInfo
            {
                FileName = MinecraftDirectory,
                UseShellExecute = true
            };
            System.Diagnostics.Process.Start(psi);
        }
        catch (Exception ex) { SaveStatus = $"Erro: {ex.Message}"; SaveStatusIsSuccess = false; }
    }

    private void DetectJava()
    {
        var detected = _javaService.FindJavaPath();
        if (!string.IsNullOrWhiteSpace(detected))
        {
            JavaPath = detected;
            SaveStatus = StatusJavaDetected;
            SaveStatusIsSuccess = true;
        }
        else { SaveStatus = StatusJavaNotDetected; SaveStatusIsSuccess = false; }
    }

    private async Task SelectJavaAsync()
    {
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var window = desktop.MainWindow;
            if (window is null) return;

            var files = await window.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "Selecionar Java",
                AllowMultiple = false,
                FileTypeFilter = new[]
                {
                    new FilePickerFileType("Java") { Patterns = new[] { "*.exe", "*.bat", "*.cmd", "*.sh" } },
                    new FilePickerFileType("Todos") { Patterns = new[] { "*" } }
                }
            });

            if (files.Count > 0)
            {
                JavaPath = files[0].TryGetLocalPath() ?? files[0].Path.ToString();
                SaveStatus = "Java selecionado";
                SaveStatusIsSuccess = true;
            }
        }
    }
}