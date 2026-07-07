using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NexusLauncher.Services;
using NexusLauncher.ViewModels.Base;
using System;
using System.Net.Http;

namespace NexusLauncher.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    public NavigationService Navigation => NavigationService.Instance;
    public CreateInstanceOverlayViewModel DialogOverlay { get; }
    public SelectInstanceOverlayViewModel SelectOverlay { get; }

    private static readonly HttpClient HttpClient = new();
    private static readonly ModrinthService ModrinthService = new(HttpClient);
    private static readonly CurseForgeService CurseForgeService = new(new HttpClient());

    [ObservableProperty]
    private string currentTab = "Play";

    [ObservableProperty]
    private string searchText = string.Empty;

    public string ProfileNickname => LauncherRuntime.Settings.Load().Nickname ?? "Player";

    public DownloadManagerService DownloadManager => DownloadManagerService.Instance;

    private PlayViewModel? _playVm;
    private ModsViewModel? _modsVm;
    private InstancesViewModel? _instancesVm;
    private ProfileViewModel? _profileVm;
    private SettingsViewModel? _settingsVm;

    public MainWindowViewModel()
    {
        var settingsService = new SettingsService();
        var instanceService = new InstanceService(settingsService);

        DialogOverlay = new CreateInstanceOverlayViewModel(instanceService);
        DialogOverlay.Closed += () => DialogOpenRequested?.Invoke();

        SelectOverlay = new SelectInstanceOverlayViewModel(instanceService);

        _playVm = new PlayViewModel();
        Navigation.NavigateTo(_playVm);
    }

    public event Action? DialogOpenRequested;

    [RelayCommand]
    private void GoPlay() { CurrentTab = "Play"; _playVm ??= new PlayViewModel(); Navigation.NavigateTo(_playVm); }

    [RelayCommand]
    private void GoMods() { CurrentTab = "Mods"; _modsVm ??= new ModsViewModel(ModrinthService, CurseForgeService, SelectOverlay); Navigation.NavigateTo(_modsVm); }

    [RelayCommand]
    private void GoInstances() { CurrentTab = "Instances"; _instancesVm ??= new InstancesViewModel(DialogOverlay); Navigation.NavigateTo(_instancesVm); }

    [RelayCommand]
    private void GoProfile() { CurrentTab = "Profile"; _profileVm ??= new ProfileViewModel(); Navigation.NavigateTo(_profileVm); }

    [RelayCommand]
    private void GoSettings() { CurrentTab = "Settings"; _settingsVm ??= new SettingsViewModel(); Navigation.NavigateTo(_settingsVm); }

    [RelayCommand]
    private void CloseDialog() { if (DialogOverlay.Visible) DialogOverlay.Visible = false; }

    [RelayCommand]
    private void CloseSelectOverlay() { if (SelectOverlay.Visible) SelectOverlay.Visible = false; }
}
