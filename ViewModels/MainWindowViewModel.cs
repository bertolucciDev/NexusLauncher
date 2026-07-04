using NexusLauncher.Services;
using NexusLauncher.ViewModels.Base;

namespace NexusLauncher.ViewModels;

public class MainWindowViewModel : ViewModelBase
{
    public NavigationService Navigation { get; } = new();

    public MainWindowViewModel()
    {
        Navigation.NavigateTo(new HomeViewModel());
    }

    public void GoHome() => Navigation.NavigateTo(new HomeViewModel());
    public void GoPlay() => Navigation.NavigateTo(new PlayViewModel());
    public void GoMods() => Navigation.NavigateTo(new ModsViewModel());
    public void GoVersions() => Navigation.NavigateTo(new VersionsViewModel());
    public void GoSettings() => Navigation.NavigateTo(new SettingsViewModel());
}