using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using NexusLauncher.Services;
using NexusLauncher.ViewModels;
using NexusLauncher.Views;

namespace NexusLauncher;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        var settings = new SettingsService().Load();
        ThemeService.Instance.Current = settings.Theme == "Light" ? AppTheme.Light : AppTheme.Dark;
        ThemeService.Instance.Apply();
        LanguageService.Instance.ApplyByCode(settings.Language);

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow
            {
                DataContext = new MainWindowViewModel()
            };
        }

        base.OnFrameworkInitializationCompleted();
    }
}