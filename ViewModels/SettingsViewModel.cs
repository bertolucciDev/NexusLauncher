using CommunityToolkit.Mvvm.ComponentModel;
using NexusLauncher.Services;
using NexusLauncher.ViewModels.Base;

namespace NexusLauncher.ViewModels;

public partial class SettingsViewModel : ViewModelBase
{
    private readonly JavaService _javaService = new();
    private readonly MinecraftService _minecraftService = new();

    [ObservableProperty]
    private string javaPath = "Não detectado";

    [ObservableProperty]
    private string minecraftDirectory = string.Empty;

    public SettingsViewModel()
    {
        JavaPath = _javaService.FindJavaPath() ?? "Não detectado";
        MinecraftDirectory = _minecraftService.MinecraftDirectory;
    }
}
