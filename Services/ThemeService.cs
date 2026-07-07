using Avalonia;
using Avalonia.Styling;
using CommunityToolkit.Mvvm.ComponentModel;

namespace NexusLauncher.Services;

public partial class ThemeService : ObservableObject
{
    private static readonly ThemeService _instance = new();
    public static ThemeService Instance => _instance;

    [ObservableProperty]
    private AppTheme current = AppTheme.Dark;

    public string CurrentKey => Current == AppTheme.Dark ? "Dark" : "Light";

    public void Apply()
    {
        if (Application.Current is null) return;
        Application.Current.RequestedThemeVariant = Current == AppTheme.Dark
            ? ThemeVariant.Dark
            : ThemeVariant.Light;
    }
}