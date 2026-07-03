using CommunityToolkit.Mvvm.ComponentModel;
using NexusLauncher.ViewModels.Base;

namespace NexusLauncher.Services;

public class NavigationService : ObservableObject
{
    private ViewModelBase? _currentView;

    public ViewModelBase? CurrentView
    {
        get => _currentView;
        set => SetProperty(ref _currentView, value);
    }

    public void NavigateTo(ViewModelBase viewModel)
    {
        CurrentView = viewModel;
    }
}