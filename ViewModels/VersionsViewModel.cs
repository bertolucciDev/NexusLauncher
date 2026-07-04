using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NexusLauncher.Models;
using NexusLauncher.Services;
using NexusLauncher.ViewModels.Base;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace NexusLauncher.ViewModels;

public partial class VersionsViewModel : ViewModelBase
{
    private readonly VersionService _versionService = new();
    private readonly Dictionary<MinecraftVersionCategory, List<MinecraftVersionInfo>> _all = new();

    [ObservableProperty] private string status = "Carregando versões...";
    [ObservableProperty] private string searchText = string.Empty;
    [ObservableProperty] private MinecraftVersionInfo? lastPlayedVersion;

    public ObservableCollection<MinecraftVersionInfo> Releases { get; } = new();
    public ObservableCollection<MinecraftVersionInfo> Snapshots { get; } = new();
    public ObservableCollection<MinecraftVersionInfo> Forge { get; } = new();
    public ObservableCollection<MinecraftVersionInfo> NeoForge { get; } = new();
    public ObservableCollection<MinecraftVersionInfo> Fabric { get; } = new();
    public ObservableCollection<MinecraftVersionInfo> Quilt { get; } = new();
    public ObservableCollection<MinecraftVersionInfo> OptiFine { get; } = new();
    public ObservableCollection<MinecraftVersionInfo> LiteLoader { get; } = new();
    public ObservableCollection<MinecraftVersionInfo> Modpacks { get; } = new();
    public ObservableCollection<MinecraftVersionInfo> Custom { get; } = new();

    public VersionsViewModel() => _ = LoadAsync();
    partial void OnSearchTextChanged(string value) => ApplySearch();
    partial void OnLastPlayedVersionChanged(MinecraftVersionInfo? value) { OnPropertyChanged(nameof(HasLastPlayedVersion)); OnPropertyChanged(nameof(LastPlayedVersionName)); }
    public bool HasLastPlayedVersion => LastPlayedVersion is not null;
    public string LastPlayedVersionName => LastPlayedVersion?.Id ?? string.Empty;

    [RelayCommand]
    private void ToggleFavorite(MinecraftVersionInfo version)
    {
        _versionService.ToggleFavorite(version.Id);
        version.IsFavorite = !version.IsFavorite;
        foreach (var key in _all.Keys.ToList()) _all[key] = Reorder(_all[key]);
        ApplySearch();
    }

    private async Task LoadAsync()
    {
        var installed = _versionService.GetInstalledVersionInfos();
        foreach (MinecraftVersionCategory category in Enum.GetValues()) _all[category] = installed.Where(v => v.Category == category).ToList();
        var official = await _versionService.GetOfficialVersionsAsync();
        _all[MinecraftVersionCategory.Release] = Reorder(_all[MinecraftVersionCategory.Release].Concat(official.Where(v => v.Category == MinecraftVersionCategory.Release)).GroupBy(v => v.Id, StringComparer.OrdinalIgnoreCase).Select(g => g.First()).Take(120));
        _all[MinecraftVersionCategory.Snapshot] = Reorder(_all[MinecraftVersionCategory.Snapshot].Concat(official.Where(v => v.Category == MinecraftVersionCategory.Snapshot)).GroupBy(v => v.Id, StringComparer.OrdinalIgnoreCase).Select(g => g.First()).Take(80));
        LastPlayedVersion = installed.FirstOrDefault(v => v.IsLastPlayed) ?? official.FirstOrDefault(v => v.IsLastPlayed);
        ApplySearch();
        Status = $"{installed.Count} instaladas • {Modpacks.Count} modpacks • {Custom.Count} customizadas";
    }

    private void ApplySearch()
    {
        Fill(Releases, Filter(MinecraftVersionCategory.Release)); Fill(Snapshots, Filter(MinecraftVersionCategory.Snapshot)); Fill(Forge, Filter(MinecraftVersionCategory.Forge));
        Fill(NeoForge, Filter(MinecraftVersionCategory.NeoForge)); Fill(Fabric, Filter(MinecraftVersionCategory.Fabric)); Fill(Quilt, Filter(MinecraftVersionCategory.Quilt));
        Fill(OptiFine, Filter(MinecraftVersionCategory.OptiFine)); Fill(LiteLoader, Filter(MinecraftVersionCategory.LiteLoader)); Fill(Modpacks, Filter(MinecraftVersionCategory.Modpack)); Fill(Custom, Filter(MinecraftVersionCategory.Custom));
    }

    private IEnumerable<MinecraftVersionInfo> Filter(MinecraftVersionCategory category)
    {
        var versions = _all.TryGetValue(category, out var values) ? values : Enumerable.Empty<MinecraftVersionInfo>();
        if (string.IsNullOrWhiteSpace(SearchText)) return versions;
        var query = SearchText.Trim().ToLowerInvariant();
        return versions.Where(v => v.Id.ToLowerInvariant().Contains(query) || v.BaseVersion.ToLowerInvariant().Contains(query) || v.Loader.ToLowerInvariant().Contains(query));
    }

    private static List<MinecraftVersionInfo> Reorder(IEnumerable<MinecraftVersionInfo> versions) => versions.OrderByDescending(v => v.IsFavorite).ThenBy(v => v.Id, StringComparer.CurrentCultureIgnoreCase).ToList();
    private static void Fill(ObservableCollection<MinecraftVersionInfo> target, IEnumerable<MinecraftVersionInfo> values) { target.Clear(); foreach (var value in values) target.Add(value); }
}
