using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NexusLauncher.Models;
using NexusLauncher.Services;
using NexusLauncher.ViewModels.Base;
using System.Collections.ObjectModel;

namespace NexusLauncher.ViewModels;

public partial class SettingsViewModel : ViewModelBase
{
    private readonly JavaService _javaService = new();
    private readonly MinecraftService _minecraftService = new();
    private readonly SettingsService _settingsService = new();

    [ObservableProperty]
    private string javaPath = "Não detectado";

    [ObservableProperty]
    private string minecraftDirectory = string.Empty;

    [ObservableProperty]
    private double allocatedRamGb;

    [ObservableProperty]
    private int maxRamGb;

    [ObservableProperty]
    private bool closeLauncherOnGameStart;

    [ObservableProperty]
    private bool reopenLauncherAfterGameClose;

    [ObservableProperty]
    private bool minimizeLauncherOnGameStart;

    [ObservableProperty]
    private bool fullscreen;

    [ObservableProperty]
    private string selectedResolution = "1280x720";

    [ObservableProperty]
    private string saveStatus = "Pronto";

    public ObservableCollection<string> Resolutions { get; } = new()
    {
        "1280x720",
        "1600x900",
        "1920x1080"
    };

    public string AllocatedRamText => $"Allocated: {(int)AllocatedRamGb}GB";

    public SettingsViewModel()
    {
        var settings = _settingsService.Load();
        MaxRamGb = _settingsService.GetMaxRamGb();
        AllocatedRamGb = settings.AllocatedRamGb;
        CloseLauncherOnGameStart = settings.CloseLauncherOnGameStart;
        ReopenLauncherAfterGameClose = settings.ReopenLauncherAfterGameClose;
        MinimizeLauncherOnGameStart = settings.MinimizeLauncherOnGameStart;
        Fullscreen = settings.Fullscreen;
        SelectedResolution = settings.Resolution;
        JavaPath = string.IsNullOrWhiteSpace(settings.JavaPath) ? _javaService.FindJavaPath() ?? "Não detectado" : settings.JavaPath;
        MinecraftDirectory = settings.MinecraftDirectory;
    }

    partial void OnAllocatedRamGbChanged(double value)
    {
        OnPropertyChanged(nameof(AllocatedRamText));
    }

    [RelayCommand]
    private void Save()
    {
        var settings = _settingsService.Load();
        settings.AllocatedRamGb = (int)AllocatedRamGb;
        settings.CloseLauncherOnGameStart = CloseLauncherOnGameStart;
        settings.ReopenLauncherAfterGameClose = ReopenLauncherAfterGameClose;
        settings.MinimizeLauncherOnGameStart = MinimizeLauncherOnGameStart;
        settings.Fullscreen = Fullscreen;
        settings.Resolution = SelectedResolution;
        settings.MinecraftDirectory = MinecraftDirectory;
        settings.JavaPath = JavaPath == "Não detectado" ? string.Empty : JavaPath;
        _settingsService.Save(settings);
        SaveStatus = "Configurações salvas";
    }
}
