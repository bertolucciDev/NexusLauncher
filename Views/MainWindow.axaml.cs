using Avalonia.Controls;
using Avalonia.Interactivity;
using NexusLauncher.ViewModels;

namespace NexusLauncher.Views;

public partial class MainWindow : Window
{
    private MainWindowViewModel ViewModel => (MainWindowViewModel)DataContext!;

    public MainWindow()
    {
        InitializeComponent();
    }

    private void HomeClick(object? sender, RoutedEventArgs e)
        => ViewModel.GoHome();

    private void PlayClick(object? sender, RoutedEventArgs e)
        => ViewModel.GoPlay();

    private void SettingsClick(object? sender, RoutedEventArgs e)
        => ViewModel.GoSettings();
}