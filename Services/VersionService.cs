using NexusLauncher.Minecraft;
using NexusLauncher.Models;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace NexusLauncher.Services;

public class VersionService
{
    private readonly HttpClient _httpClient = new();
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

            var installed = GetInstalledVersions().ToHashSet();
            return manifest.Versions
                .Where(v => !string.IsNullOrWhiteSpace(v.Id))
                .Select(v => new MinecraftVersionInfo
                {
                    Id = v.Id!,
                    Type = v.Type ?? "release",
                    IsInstalled = installed.Contains(v.Id!),
                    Category = MinecraftVersionCategory.Release
                })
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
            .OrderByDescending(name => name)
            .ToList()!;
    }

    public List<MinecraftVersionInfo> GetInstalledVersionInfos()
    {
        return GetInstalledVersions()
            .Select(id => new MinecraftVersionInfo
            {
                Id = id,
                Type = DetectType(id),
                IsInstalled = true,
                Category = DetectCategory(id)
            })
            .ToList();
    }

    public List<MinecraftVersionInfo> GetModdedVersions()
    {
        return GetInstalledVersionInfos()
            .Where(v => v.Category is MinecraftVersionCategory.Modded or MinecraftVersionCategory.OptiFine)
            .ToList();
    }

    public List<MinecraftVersionInfo> GetOptiFineVersions()
    {
        return GetInstalledVersionInfos()
            .Where(v => v.Category == MinecraftVersionCategory.OptiFine)
            .ToList();
    }

    public bool IsVersionInstalled(string version)
    {
        return !string.IsNullOrWhiteSpace(version) && Directory.Exists(Path.Combine(versionsPath, version));
    }

    private MinecraftVersionCategory DetectCategory(string id)
    {
        var lower = id.ToLowerInvariant();
        if (lower.Contains("optifine"))
            return MinecraftVersionCategory.OptiFine;

        if (lower.Contains("forge") || lower.Contains("fabric") || lower.Contains("quilt") || lower.Contains("tl") || HasCustomJson(id))
            return MinecraftVersionCategory.Modded;

        return MinecraftVersionCategory.Installed;
    }

    private string DetectType(string id)
    {
        return DetectCategory(id) switch
        {
            MinecraftVersionCategory.OptiFine => "optifine",
            MinecraftVersionCategory.Modded => "modded",
            _ => "installed"
        };
    }

    private bool HasCustomJson(string id)
    {
        var jsonPath = Path.Combine(versionsPath, id, $"{id}.json");
        if (!File.Exists(jsonPath))
            return false;

        try
        {
            var json = File.ReadAllText(jsonPath).ToLowerInvariant();
            return json.Contains("forge") || json.Contains("fabric-loader") || json.Contains("optifine") || json.Contains("tlauncher") || json.Contains("inheritsfrom");
        }
        catch
        {
            return false;
        }
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
}
