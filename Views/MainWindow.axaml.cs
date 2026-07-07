using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using NexusLauncher.Services;
using NexusLauncher.ViewModels;
using System.ComponentModel;

namespace NexusLauncher.Views;

public partial class MainWindow : Window
{
    private MainWindowViewModel ViewModel => (MainWindowViewModel)DataContext!;
    private TrayIcon? _trayIcon;

    public MainWindow()
    {
        InitializeComponent();
        DataContextChanged += (_, _) =>
        {
            if (DataContext is MainWindowViewModel vm)
            {
                vm.PropertyChanged += OnViewModelPropertyChanged;
                UpdateActiveNav(vm.CurrentTab);
            }
        };

        LauncherRuntime.Processes.MinecraftStarted += OnMinecraftStarted;
        LauncherRuntime.Processes.MinecraftExited += OnMinecraftExited;
    }

    protected override void OnClosing(WindowClosingEventArgs e)
    {
        if (LauncherRuntime.Processes.IsGameRunning)
        {
            var settings = LauncherRuntime.Settings.Load();
            if (settings.KeepLauncherInBackground)
            {
                e.Cancel = true;
                HideToTray();
                return;
            }
        }

        _trayIcon?.Dispose();
        _trayIcon = null;
        base.OnClosing(e);
    }

    private void OnMinecraftStarted(object? sender, System.EventArgs e)
    {
        var settings = LauncherRuntime.Settings.Load();

        if (!settings.CloseLauncherOnGameStart && !settings.MinimizeLauncherOnGameStart)
            return;

        Avalonia.Threading.Dispatcher.UIThread.Post(HideToTray);
    }

    private void HideToTray()
    {
        if (_trayIcon is null)
        {
            var icon = new WindowIcon(
                "avares://NexusLauncher/Assets/Images/NexusLogo.png");
            var menu = new NativeMenu();
            menu.Items.Add(new NativeMenuItem("Abrir Nexus Launcher")
            {
                Command = new CommunityToolkit.Mvvm.Input.RelayCommand(RestoreWindow)
            });
            menu.Items.Add(new NativeMenuItem("Sair")
            {
                Command = new CommunityToolkit.Mvvm.Input.RelayCommand(() =>
                {
                    _trayIcon?.Dispose();
                    _trayIcon = null;
                    if (Application.Current?.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop)
                        desktop.Shutdown();
                })
            });

            _trayIcon = new TrayIcon
            {
                Icon = icon,
                ToolTipText = "Nexus Launcher",
                Menu = menu
            };
            _trayIcon.Clicked += (_, _) => RestoreWindow();
        }

        _trayIcon.IsVisible = true;
        Hide();
    }

    private void OnMinecraftExited(object? sender, System.EventArgs e)
    {
        var settings = LauncherRuntime.Settings.Load();

        if (!settings.ReopenLauncherAfterGameClose)
            return;

        Avalonia.Threading.Dispatcher.UIThread.Post(RestoreWindow);
    }

    private void RestoreWindow()
    {
        if (_trayIcon is not null)
            _trayIcon.IsVisible = false;

        Show();
        WindowState = WindowState.Normal;
        Activate();
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MainWindowViewModel.CurrentTab))
            UpdateActiveNav(((MainWindowViewModel)sender!).CurrentTab);
    }

    private void UpdateActiveNav(string tab)
    {
        foreach (var btn in new[] { NavPlay, NavMods, NavInstances, NavProfile, NavSettings })
            btn.Classes.Remove("Active");

        var target = tab switch
        {
            "Play" => NavPlay,
            "Mods" => NavMods,
            "Instances" => NavInstances,
            "Profile" => NavProfile,
            "Settings" => NavSettings,
            _ => null
        };
        target?.Classes.Add("Active");
    }

    private void PlayClick(object? sender, RoutedEventArgs e)
        => ViewModel.GoPlayCommand.Execute(null);

    private void ModsClick(object? sender, RoutedEventArgs e)
        => ViewModel.GoModsCommand.Execute(null);

    private void InstancesClick(object? sender, RoutedEventArgs e)
        => ViewModel.GoInstancesCommand.Execute(null);

    private void ProfileClick(object? sender, RoutedEventArgs e)
        => ViewModel.GoProfileCommand.Execute(null);

    private void SettingsClick(object? sender, RoutedEventArgs e)
        => ViewModel.GoSettingsCommand.Execute(null);
}
