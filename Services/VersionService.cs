using NexusLauncher.Minecraft;
using NexusLauncher.Models;
using System;
using System.Collections.Generic;
using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace NexusLauncher.Services;

public class VersionService
{
    private static readonly string[] ModpackMarkers = { "manifest.json", "modrinth.index.json", "instance.cfg", "mmc-pack.json" };
    private readonly HttpClient _httpClient = new();
    private readonly SettingsService _settingsService = new();
    private readonly string versionsPath = MinecraftPaths.GamePath.Versions;

    public VersionService() => Directory.CreateDirectory(versionsPath);

    public async Task<List<MinecraftVersionInfo>> GetOfficialVersionsAsync()
    {
        try
        {
            var json = await _httpClient.GetStringAsync("https://launchermeta.mojang.com/mc/game/version_manifest.json");
            var manifest = JsonSerializer.Deserialize<VersionManifest>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            if (manifest?.Versions == null) return new();

            var installed = GetInstalledVersions().ToHashSet(StringComparer.OrdinalIgnoreCase);
            var settings = _settingsService.Load();
            var favorites = settings.FavoriteVersions.ToHashSet(StringComparer.OrdinalIgnoreCase);

            return manifest.Versions.Where(v => !string.IsNullOrWhiteSpace(v.Id)).Select(v => new MinecraftVersionInfo
            {
                Id = v.Id!,
                Type = v.Type ?? "release",
                BaseVersion = v.Id!,
                Loader = "Vanilla",
                BadgeIcon = v.Type == "snapshot" ? "🧪" : "🟩",
                IsInstalled = installed.Contains(v.Id!),
                IsFavorite = favorites.Contains(v.Id!),
                IsLastPlayed = string.Equals(settings.LastPlayedVersion, v.Id, StringComparison.OrdinalIgnoreCase),
                Category = v.Type == "snapshot" ? MinecraftVersionCategory.Snapshot : MinecraftVersionCategory.Release
            }).OrderByDescending(v => v.IsFavorite).ThenByDescending(v => v.Id, VersionStringComparer.Instance).ToList();
        }
        catch { return new(); }
    }

    public List<string> GetInstalledVersions() => !Directory.Exists(versionsPath) ? new() : Directory.GetDirectories(versionsPath).Select(Path.GetFileName).Where(n => !string.IsNullOrWhiteSpace(n)).ToList()!;

    public List<MinecraftVersionInfo> GetInstalledVersionInfos()
    {
        var settings = _settingsService.Load();
        var favorites = settings.FavoriteVersions.ToHashSet(StringComparer.OrdinalIgnoreCase);
        return GetInstalledVersions().Select(id => AnalyzeLocalVersion(id, settings.LastPlayedVersion, favorites))
            .OrderByDescending(v => v.IsFavorite).ThenBy(v => v.Category).ThenBy(v => v.Id, StringComparer.CurrentCultureIgnoreCase).ToList();
    }

    public bool IsVersionInstalled(string version) => !string.IsNullOrWhiteSpace(version) && File.Exists(Path.Combine(versionsPath, version, $"{version}.json"));

    public void ToggleFavorite(string versionId)
    {
        if (string.IsNullOrWhiteSpace(versionId)) return;
        var settings = _settingsService.Load();
        var existing = settings.FavoriteVersions.FirstOrDefault(v => string.Equals(v, versionId, StringComparison.OrdinalIgnoreCase));
        if (existing is null) settings.FavoriteVersions.Add(versionId); else settings.FavoriteVersions.Remove(existing);
        _settingsService.Save(settings);
    }

    public MinecraftVersionInfo AnalyzeLocalVersion(string id, string lastPlayedVersion = "", HashSet<string>? favorites = null)
    {
        favorites ??= new(StringComparer.OrdinalIgnoreCase);
        var versionDir = Path.Combine(versionsPath, id);
        var jsonPath = Path.Combine(versionDir, $"{id}.json");
        using var doc = TryParse(jsonPath);
        var signals = CollectSignals(versionDir, doc?.RootElement);
        var category = DetectCategory(id, signals);
        var loader = CategoryToLoader(category);

        return new MinecraftVersionInfo
        {
            Id = id,
            Type = category is MinecraftVersionCategory.Release ? "release" : category is MinecraftVersionCategory.Snapshot ? "snapshot" : "custom",
            IsInstalled = true,
            Category = category,
            BaseVersion = DetectBaseVersion(doc?.RootElement, id),
            Loader = loader,
            LoaderVersion = DetectLoaderVersion(signals, loader),
            BadgeIcon = GetBadge(category),
            IsFavorite = favorites.Contains(id),
            IsLastPlayed = string.Equals(lastPlayedVersion, id, StringComparison.OrdinalIgnoreCase)
        };
    }

    private static HashSet<string> CollectSignals(string versionDir, JsonElement? root)
    {
        var signals = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var marker in ModpackMarkers.Where(m => File.Exists(Path.Combine(versionDir, m)))) signals.Add($"marker:{marker}");
        if (Directory.Exists(Path.Combine(versionDir, "mods"))) signals.Add("folder:mods");
        if (root is null) return signals;
        AddJsonString(root.Value, signals);
        if (root.Value.TryGetProperty("libraries", out var libs) && libs.ValueKind == JsonValueKind.Array)
            foreach (var lib in libs.EnumerateArray()) if (TryGetString(lib, "name", out var name)) signals.Add($"library:{name}");
        return signals;
    }

    private static void AddJsonString(JsonElement element, HashSet<string> signals)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                foreach (var property in element.EnumerateObject()) { signals.Add($"key:{property.Name}"); AddJsonString(property.Value, signals); }
                break;
            case JsonValueKind.Array:
                foreach (var item in element.EnumerateArray()) AddJsonString(item, signals);
                break;
            case JsonValueKind.String:
                var value = element.GetString(); if (!string.IsNullOrWhiteSpace(value)) signals.Add(value); break;
        }
    }

    private static MinecraftVersionCategory DetectCategory(string id, HashSet<string> signals)
    {
        bool Has(string value) => signals.Any(s => s.Contains(value, StringComparison.OrdinalIgnoreCase)) || id.Contains(value, StringComparison.OrdinalIgnoreCase);
        if (Has("modrinth.index.json") || Has("mmc-pack.json") || Has("instance.cfg") || Has("curseforge") || Has("manifest.json")) return MinecraftVersionCategory.Modpack;
        if (Has("neoforge")) return MinecraftVersionCategory.NeoForge;
        if (Has("fabric-loader") || Has("net.fabricmc") || Has("fabric")) return MinecraftVersionCategory.Fabric;
        if (Has("quilt-loader") || Has("org.quiltmc") || Has("quilt")) return MinecraftVersionCategory.Quilt;
        if (Has("optifine")) return MinecraftVersionCategory.OptiFine;
        if (Has("liteloader")) return MinecraftVersionCategory.LiteLoader;
        if (Has("forge")) return MinecraftVersionCategory.Forge;
        if (!LooksLikeVanillaVersion(id) || Has("folder:mods")) return MinecraftVersionCategory.Custom;
        if (id.Contains("w", StringComparison.OrdinalIgnoreCase) || id.Contains("snapshot", StringComparison.OrdinalIgnoreCase)) return MinecraftVersionCategory.Snapshot;
        return MinecraftVersionCategory.Release;
    }

    private static string DetectBaseVersion(JsonElement? root, string fallback)
    {
        if (root is not null)
        {
            if (TryGetString(root.Value, "inheritsFrom", out var inherits)) return inherits;
            if (TryGetString(root.Value, "releaseTarget", out var target)) return target;
            if (TryGetString(root.Value, "id", out var id) && LooksLikeVanillaVersion(id)) return id;
        }
        return LooksLikeVanillaVersion(fallback) ? fallback : string.Empty;
    }

    private static string DetectLoaderVersion(HashSet<string> signals, string loader)
    {
        if (loader is "Vanilla" or "Custom" or "Modpack") return string.Empty;
        var library = signals.FirstOrDefault(s => s.StartsWith("library:", StringComparison.OrdinalIgnoreCase) && s.Contains(loader, StringComparison.OrdinalIgnoreCase));
        var parts = library?.Split(':', StringSplitOptions.RemoveEmptyEntries);
        return parts is { Length: > 2 } ? parts[^1] : string.Empty;
    }

    private static string CategoryToLoader(MinecraftVersionCategory c) => c switch
    {
        MinecraftVersionCategory.Forge => "Forge", MinecraftVersionCategory.NeoForge => "NeoForge", MinecraftVersionCategory.Fabric => "Fabric",
        MinecraftVersionCategory.Quilt => "Quilt", MinecraftVersionCategory.OptiFine => "OptiFine", MinecraftVersionCategory.LiteLoader => "LiteLoader",
        MinecraftVersionCategory.Modpack => "Modpack", MinecraftVersionCategory.Custom => "Custom", _ => "Vanilla"
    };

    private static string GetBadge(MinecraftVersionCategory c) => c switch
    {
        MinecraftVersionCategory.Snapshot => "🧪", MinecraftVersionCategory.Forge => "🟧", MinecraftVersionCategory.NeoForge => "🟪", MinecraftVersionCategory.Fabric => "🟦",
        MinecraftVersionCategory.Quilt => "⬜", MinecraftVersionCategory.OptiFine => "🟨", MinecraftVersionCategory.LiteLoader => "🟫", MinecraftVersionCategory.Modpack => "📦",
        MinecraftVersionCategory.Custom => "🛠", _ => "🟩"
    };

    private static JsonDocument? TryParse(string path) { try { return File.Exists(path) ? JsonDocument.Parse(File.ReadAllText(path)) : null; } catch { return null; } }
    private static bool TryGetString(JsonElement e, string name, out string value) { value = string.Empty; if (e.TryGetProperty(name, out var p) && p.ValueKind == JsonValueKind.String) { value = p.GetString() ?? string.Empty; return !string.IsNullOrWhiteSpace(value); } return false; }
    private static bool LooksLikeVanillaVersion(string value) => !string.IsNullOrWhiteSpace(value) && value.All(c => char.IsDigit(c) || c == '.' || c == '-');

    private sealed class VersionManifest { public List<VersionEntry>? Versions { get; set; } }
    private sealed class VersionEntry { public string? Id { get; set; } public string? Type { get; set; } }

    private sealed class VersionStringComparer : IComparer<string>
    {
        public static VersionStringComparer Instance { get; } = new();
        public int Compare(string? x, string? y) => string.Compare(x, y, StringComparison.OrdinalIgnoreCase);
    }
}
