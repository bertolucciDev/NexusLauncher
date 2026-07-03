using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace NexusLauncher.Minecraft.Versions;

public class VersionManager
{
    private readonly string _root;
    private readonly HttpClient _httpClient = new();

    public VersionManager(string? root = null)
    {
        _root = root ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), ".minecraft", "versions");
        Directory.CreateDirectory(_root);
    }

    public List<string> GetInstalledVersions()
    {
        return Directory.GetDirectories(_root)
            .Select(Path.GetFileName)
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .OrderBy(name => name)
            .ToList()!;
    }

    public bool IsInstalled(string version)
    {
        return Directory.Exists(Path.Combine(_root, version));
    }

    public async Task<string?> DownloadVersionAsync(string version)
    {
        var manifestUrl = $"https://launchermeta.mojang.com/mc/game/version_manifest.json";
        var manifest = await _httpClient.GetStringAsync(manifestUrl);
        var root = JsonSerializer.Deserialize<VersionManifest>(manifest, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        var selected = root?.Versions?.FirstOrDefault(v => v.Id == version);
        if (selected is null) return null;

        var versionJsonUrl = selected.Url;
        var versionJson = await _httpClient.GetStringAsync(versionJsonUrl);
        var versionData = JsonSerializer.Deserialize<VersionJson>(versionJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        var versionDir = Path.Combine(_root, version);
        Directory.CreateDirectory(versionDir);
        await File.WriteAllTextAsync(Path.Combine(versionDir, $"{version}.json"), versionJson);

        return versionData?.Id;
    }

    private sealed class VersionManifest
    {
        public List<VersionEntry>? Versions { get; set; }
    }

    private sealed class VersionEntry
    {
        public string? Id { get; set; }
        public string? Url { get; set; }
    }

    private sealed class VersionJson
    {
        public string? Id { get; set; }
    }
}
