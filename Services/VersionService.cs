using NexusLauncher.Minecraft;
using NexusLauncher.Models;
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
    private static readonly string[] ModpackMarkers =
    {
        "manifest.json",
        "modrinth.index.json",
        "instance.cfg",
        "mmc-pack.json",
        "launcher_profiles.json"
    };

    private readonly HttpClient _httpClient = new();
    private readonly SettingsService _settingsService = new();
    private readonly string versionsPath = MinecraftPaths.GamePath.Versions;

    public VersionService()
    {
        Directory.CreateDirectory(versionsPath);
    }

    public async Task<List<MinecraftVersionInfo>> GetOfficialVersionsAsync()
    {
        try
        {
            const string url = "https://launchermeta.mojang.com/mc/game/version_manifest.json";
            var json = await _httpClient.GetStringAsync(url);
            var manifest = JsonSerializer.Deserialize<VersionManifest>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (manifest?.Versions == null)
                return new List<MinecraftVersionInfo>();

            var installed = GetInstalledVersions().ToHashSet(StringComparer.OrdinalIgnoreCase);
            var settings = _settingsService.Load();
            var favorites = settings.FavoriteVersions.ToHashSet(StringComparer.OrdinalIgnoreCase);

            return manifest.Versions
                .Where(v => !string.IsNullOrWhiteSpace(v.Id))
                .Select(v => new MinecraftVersionInfo
                {
                    Id = v.Id!,
                    Type = v.Type ?? "release",
                    BaseVersion = v.Id!,
                    Loader = "Vanilla",
                    BadgeIcon = "🟩",
                    IsInstalled = installed.Contains(v.Id!),
                    IsFavorite = favorites.Contains(v.Id!),
                    IsLastPlayed = string.Equals(settings.LastPlayedVersion, v.Id, StringComparison.OrdinalIgnoreCase),
                    Category = MinecraftVersionCategory.Release
                })
                .OrderByDescending(v => v.IsFavorite)
                .ThenByDescending(v => v.Id, VersionStringComparer.Instance)
                .ToList();
        }
        catch
        {
            return new List<MinecraftVersionInfo>();
        }
    }

    public List<string> GetInstalledVersions()
    {
        if (!Directory.Exists(versionsPath))
            return new List<string>();

        return Directory.GetDirectories(versionsPath)
            .Select(Path.GetFileName)
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .ToList()!;
    }

    public List<MinecraftVersionInfo> GetInstalledVersionInfos()
    {
        var settings = _settingsService.Load();
        var favorites = settings.FavoriteVersions.ToHashSet(StringComparer.OrdinalIgnoreCase);

        return GetInstalledVersions()
            .Select(id => AnalyzeLocalVersion(id, settings.LastPlayedVersion, favorites))
            .OrderByDescending(v => v.IsFavorite)
            .ThenBy(v => v.Category == MinecraftVersionCategory.Release ? 0 : 1)
            .ThenBy(v => v.Category == MinecraftVersionCategory.Release ? v.Id : string.Empty, VersionStringComparer.Instance.Descending())
            .ThenBy(v => v.Category == MinecraftVersionCategory.Release ? string.Empty : v.Id, StringComparer.CurrentCultureIgnoreCase)
            .ToList();
    }

    public List<MinecraftVersionInfo> GetModdedVersions()
    {
        return GetInstalledVersionInfos()
            .Where(v => v.Category is MinecraftVersionCategory.Modded or MinecraftVersionCategory.OptiFine)
            .OrderByDescending(v => v.IsFavorite)
            .ThenBy(v => v.Id, StringComparer.CurrentCultureIgnoreCase)
            .ToList();
    }

    public List<MinecraftVersionInfo> GetOptiFineVersions()
    {
        return GetInstalledVersionInfos()
            .Where(v => v.Category == MinecraftVersionCategory.OptiFine)
            .OrderByDescending(v => v.IsFavorite)
            .ThenBy(v => v.Id, StringComparer.CurrentCultureIgnoreCase)
            .ToList();
    }

    public bool IsVersionInstalled(string version)
    {
        return !string.IsNullOrWhiteSpace(version) && Directory.Exists(Path.Combine(versionsPath, version));
    }

    public void ToggleFavorite(string versionId)
    {
        if (string.IsNullOrWhiteSpace(versionId))
            return;

        var settings = _settingsService.Load();
        var existing = settings.FavoriteVersions.FirstOrDefault(v => string.Equals(v, versionId, StringComparison.OrdinalIgnoreCase));
        if (existing is null)
            settings.FavoriteVersions.Add(versionId);
        else
            settings.FavoriteVersions.Remove(existing);

        _settingsService.Save(settings);
    }

    private MinecraftVersionInfo AnalyzeLocalVersion(string id, string lastPlayedVersion, HashSet<string> favorites)
    {
        var versionDir = Path.Combine(versionsPath, id);
        var jsonPath = Path.Combine(versionDir, $"{id}.json");
        var json = TryRead(jsonPath);
        var jsonLower = json.ToLowerInvariant();
        var hasModsFolder = Directory.Exists(Path.Combine(versionDir, "mods")) || Directory.Exists(Path.Combine(MinecraftPaths.Root, "mods", id));
        var hasModpackMarkers = ModpackMarkers.Any(marker => File.Exists(Path.Combine(versionDir, marker)));
        var loader = DetectLoader(jsonLower, hasModsFolder, hasModpackMarkers);
        var category = loader == "Vanilla" && !hasModsFolder && !hasModpackMarkers
            ? MinecraftVersionCategory.Release
            : loader == "OptiFine" ? MinecraftVersionCategory.OptiFine : MinecraftVersionCategory.Modded;

        return new MinecraftVersionInfo
        {
            Id = id,
            Type = category == MinecraftVersionCategory.Release ? "release" : "modded",
            IsInstalled = true,
            Category = category,
            BaseVersion = DetectBaseVersion(json, id),
            Loader = loader,
            LoaderVersion = DetectLoaderVersion(json, loader),
            BadgeIcon = GetBadge(loader, category),
            IsFavorite = favorites.Contains(id),
            IsLastPlayed = string.Equals(lastPlayedVersion, id, StringComparison.OrdinalIgnoreCase)
        };
    }

    private static string DetectLoader(string jsonLower, bool hasModsFolder, bool hasModpackMarkers)
    {
        if (jsonLower.Contains("neoforge")) return "NeoForge";
        if (jsonLower.Contains("fabric-loader")) return "Fabric";
        if (jsonLower.Contains("quilt")) return "Quilt";
        if (jsonLower.Contains("optifine")) return "OptiFine";
        if (jsonLower.Contains("forge") || jsonLower.Contains("inheritsfrom")) return "Forge";
        if (hasModsFolder || hasModpackMarkers) return "Modded";
        return "Vanilla";
    }

    private static string DetectBaseVersion(string json, string fallback)
    {
        if (string.IsNullOrWhiteSpace(json))
            return LooksLikeVanillaVersion(fallback) ? fallback : string.Empty;

        try
        {
            using var document = JsonDocument.Parse(json);
            var root = document.RootElement;
            if (TryGetString(root, "inheritsFrom", out var inheritsFrom))
                return inheritsFrom;
            if (TryGetString(root, "releaseTarget", out var releaseTarget))
                return releaseTarget;
            if (TryGetString(root, "id", out var id) && LooksLikeVanillaVersion(id))
                return id;
        }
        catch
        {
            return LooksLikeVanillaVersion(fallback) ? fallback : string.Empty;
        }

        return LooksLikeVanillaVersion(fallback) ? fallback : string.Empty;
    }

    private static string DetectLoaderVersion(string json, string loader)
    {
        if (string.IsNullOrWhiteSpace(json) || loader is "Vanilla" or "Modded")
            return string.Empty;

        try
        {
            using var document = JsonDocument.Parse(json);
            if (!document.RootElement.TryGetProperty("libraries", out var libraries) || libraries.ValueKind != JsonValueKind.Array)
                return string.Empty;

            foreach (var library in libraries.EnumerateArray())
            {
                if (!TryGetString(library, "name", out var name))
                    continue;

                var lower = name.ToLowerInvariant();
                if (!lower.Contains(loader.ToLowerInvariant()))
                    continue;

                var parts = name.Split(':');
                return parts.Length > 2 ? parts[^1] : string.Empty;
            }
        }
        catch
        {
            return string.Empty;
        }

        return string.Empty;
    }

    private static string GetBadge(string loader, MinecraftVersionCategory category)
    {
        return loader switch
        {
            "Forge" => "🟧",
            "Fabric" => "🟦",
            "NeoForge" => "🟪",
            "Quilt" => "⬜",
            "OptiFine" => "🟨",
            _ => category == MinecraftVersionCategory.Release ? "🟩" : "🟧"
        };
    }

    private static string TryRead(string path)
    {
        try
        {
            return File.Exists(path) ? File.ReadAllText(path) : string.Empty;
        }
        catch
        {
            return string.Empty;
        }
    }

    private static bool TryGetString(JsonElement element, string propertyName, out string value)
    {
        value = string.Empty;
        if (element.TryGetProperty(propertyName, out var property) && property.ValueKind == JsonValueKind.String)
        {
            value = property.GetString() ?? string.Empty;
            return !string.IsNullOrWhiteSpace(value);
        }

        return false;
    }

    private static bool LooksLikeVanillaVersion(string value)
    {
        return !string.IsNullOrWhiteSpace(value) && value.All(c => char.IsDigit(c) || c == '.' || c == '-');
    }

    private sealed class VersionManifest
    {
        public List<VersionEntry>? Versions { get; set; }
    }

    private sealed class VersionEntry
    {
        public string? Id { get; set; }
        public string? Type { get; set; }
    }

    private sealed class VersionStringComparer : IComparer<string>
    {
        public static VersionStringComparer Instance { get; } = new(false);
        private readonly bool _descending;

        private VersionStringComparer(bool descending)
        {
            _descending = descending;
        }

        public VersionStringComparer Descending() => new(true);

        public int Compare(string? x, string? y)
        {
            var result = CompareAscending(x ?? string.Empty, y ?? string.Empty);
            return _descending ? -result : result;
        }

        private static int CompareAscending(string x, string y)
        {
            var xParts = x.Split('.', '-');
            var yParts = y.Split('.', '-');
            var length = Math.Max(xParts.Length, yParts.Length);
            for (var i = 0; i < length; i++)
            {
                var xp = i < xParts.Length ? xParts[i] : "0";
                var yp = i < yParts.Length ? yParts[i] : "0";
                if (int.TryParse(xp, out var xi) && int.TryParse(yp, out var yi))
                {
                    var numberCompare = xi.CompareTo(yi);
                    if (numberCompare != 0) return numberCompare;
                    continue;
                }

                var textCompare = string.Compare(xp, yp, StringComparison.OrdinalIgnoreCase);
                if (textCompare != 0) return textCompare;
            }

            return 0;
        }
    }
}
