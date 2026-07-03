using CommunityToolkit.Mvvm.ComponentModel;
using NexusLauncher.Minecraft;
using NexusLauncher.ViewModels.Base;
using System.Linq;

namespace NexusLauncher.ViewModels;

public partial class HomeViewModel : ViewModelBase
{
    private readonly MinecraftService _minecraftService = new();

    [ObservableProperty]
    private string javaStatus = "Verificando...";

    [ObservableProperty]
    private string launchStatus = "Aguardando";

    [ObservableProperty]
    private string selectedVersion = "Nenhuma";

    public HomeViewModel()
    {
        Refresh();
    }

    public void Refresh()
    {
        var versions = _minecraftService.GetVersions();
        JavaStatus = _minecraftService.GetJavaStatusMessage();
        LaunchStatus = versions.Count > 0
            ? $"{versions.Count} versões disponíveis"
            : "Nenhuma versão instalada";
        SelectedVersion = versions.FirstOrDefault() ?? "Nenhuma";
    }
}