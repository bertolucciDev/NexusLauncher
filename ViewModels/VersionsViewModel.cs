using CommunityToolkit.Mvvm.ComponentModel;
using NexusLauncher.Models;
using NexusLauncher.Services;
using NexusLauncher.ViewModels.Base;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace NexusLauncher.ViewModels;

public partial class VersionsViewModel : ViewModelBase
{
    private readonly VersionService _versionService = new();

    [ObservableProperty]
    private string status = "Carregando versões...";

    public ObservableCollection<MinecraftVersionInfo> Releases { get; } = new();
    public ObservableCollection<MinecraftVersionInfo> Modded { get; } = new();
    public ObservableCollection<MinecraftVersionInfo> OptiFine { get; } = new();
    public ObservableCollection<MinecraftVersionInfo> Installed { get; } = new();

    public VersionsViewModel()
    {
        _ = LoadAsync();
    }

    private async Task LoadAsync()
    {
        Installed.Clear();
        Modded.Clear();
        OptiFine.Clear();
        Releases.Clear();

        foreach (var version in _versionService.GetInstalledVersionInfos())
        {
            Installed.Add(version);
            if (version.Category == MinecraftVersionCategory.Modded)
                Modded.Add(version);
            if (version.Category == MinecraftVersionCategory.OptiFine)
                OptiFine.Add(version);
        }

        foreach (var version in (await _versionService.GetOfficialVersionsAsync()).Where(v => v.Type == "release").Take(80))
            Releases.Add(version);

        Status = $"{Installed.Count} instaladas • {Modded.Count} modded • {OptiFine.Count} OptiFine";
    }
}
