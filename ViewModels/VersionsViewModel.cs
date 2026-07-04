using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NexusLauncher.Models;
using NexusLauncher.Services;
using NexusLauncher.ViewModels.Base;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace NexusLauncher.ViewModels;

public partial class VersionsViewModel : ViewModelBase
{
    private readonly VersionService _versionService = new();
    private List<MinecraftVersionInfo> _allReleases = new();
    private List<MinecraftVersionInfo> _allModded = new();
    private List<MinecraftVersionInfo> _allOptiFine = new();
    private List<MinecraftVersionInfo> _allInstalled = new();

    [ObservableProperty]
    private string status = "Carregando versões...";

    [ObservableProperty]
    private string searchText = string.Empty;

    [ObservableProperty]
    private MinecraftVersionInfo? lastPlayedVersion;

    public ObservableCollection<MinecraftVersionInfo> Releases { get; } = new();
    public ObservableCollection<MinecraftVersionInfo> Modded { get; } = new();
    public ObservableCollection<MinecraftVersionInfo> OptiFine { get; } = new();
    public ObservableCollection<MinecraftVersionInfo> Installed { get; } = new();

    public VersionsViewModel()
    {
        _ = LoadAsync();
    }

    partial void OnSearchTextChanged(string value)
    {
        ApplySearch();
    }

    partial void OnLastPlayedVersionChanged(MinecraftVersionInfo? value)
    {
        OnPropertyChanged(nameof(HasLastPlayedVersion));
        OnPropertyChanged(nameof(LastPlayedVersionName));
    }

    public bool HasLastPlayedVersion => LastPlayedVersion is not null;

    public string LastPlayedVersionName => LastPlayedVersion?.Id ?? string.Empty;

    [RelayCommand]
    private void ToggleFavorite(MinecraftVersionInfo version)
    {
        _versionService.ToggleFavorite(version.Id);
        version.IsFavorite = !version.IsFavorite;
        _allReleases = Reorder(_allReleases);
        _allModded = Reorder(_allModded);
        _allOptiFine = Reorder(_allOptiFine);
        _allInstalled = Reorder(_allInstalled);
        ApplySearch();
    }

    private async Task LoadAsync()
    {
        _allInstalled = _versionService.GetInstalledVersionInfos();
        _allModded = _allInstalled
            .Where(v => v.Category is MinecraftVersionCategory.Modded or MinecraftVersionCategory.OptiFine)
            .OrderByDescending(v => v.IsFavorite)
            .ThenBy(v => v.Id, System.StringComparer.CurrentCultureIgnoreCase)
            .ToList();
        _allOptiFine = _allInstalled
            .Where(v => v.Category == MinecraftVersionCategory.OptiFine)
            .OrderByDescending(v => v.IsFavorite)
            .ThenBy(v => v.Id, System.StringComparer.CurrentCultureIgnoreCase)
            .ToList();
        _allReleases = (await _versionService.GetOfficialVersionsAsync())
            .Where(v => v.Type == "release")
            .Take(120)
            .ToList();
        LastPlayedVersion = _allInstalled.FirstOrDefault(v => v.IsLastPlayed) ?? _allReleases.FirstOrDefault(v => v.IsLastPlayed);

        ApplySearch();
        Status = $"{Installed.Count} instaladas • {Modded.Count} modded • {OptiFine.Count} OptiFine";
    }

    private void ApplySearch()
    {
        Fill(Releases, Filter(_allReleases));
        Fill(Modded, Filter(_allModded));
        Fill(OptiFine, Filter(_allOptiFine));
        Fill(Installed, Filter(_allInstalled));
    }

    private IEnumerable<MinecraftVersionInfo> Filter(IEnumerable<MinecraftVersionInfo> versions)
    {
        if (string.IsNullOrWhiteSpace(SearchText))
            return versions;

        var query = SearchText.Trim().ToLowerInvariant();
        return versions.Where(v =>
            v.Id.ToLowerInvariant().Contains(query) ||
            v.BaseVersion.ToLowerInvariant().Contains(query) ||
            v.Loader.ToLowerInvariant().Contains(query));
    }

    private static List<MinecraftVersionInfo> Reorder(IEnumerable<MinecraftVersionInfo> versions)
    {
        return versions
            .OrderByDescending(v => v.IsFavorite)
            .ThenBy(v => v.Id, System.StringComparer.CurrentCultureIgnoreCase)
            .ToList();
    }

    private static void Fill(ObservableCollection<MinecraftVersionInfo> target, IEnumerable<MinecraftVersionInfo> values)
    {
        target.Clear();
        foreach (var value in values)
            target.Add(value);
    }
}
