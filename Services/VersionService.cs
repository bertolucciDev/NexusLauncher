using NexusLauncher.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace NexusLauncher.Services;

public class VersionService
{
    private readonly HttpClient _httpClient = new();

    private readonly string versionsPath =
        Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Minecraft", "Versions");

    public VersionService()
    {
        Directory.CreateDirectory(versionsPath);
    }

    // 🔥 VERSÕES OFICIAIS (MOJANG)
    public async Task<List<MinecraftVersionInfo>> GetOfficialVersionsAsync()
    {
        try
        {
            var url = "https://launchermeta.mojang.com/mc/game/version_manifest.json";

            var json = await _httpClient.GetStringAsync(url);

            var manifest = JsonSerializer.Deserialize<VersionManifest>(json,
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

            if (manifest?.Versions == null)
                return new List<MinecraftVersionInfo>();

            var installed = GetInstalledVersions();

            var result = new List<MinecraftVersionInfo>();

            foreach (var v in manifest.Versions)
            {
                result.Add(new MinecraftVersionInfo
                {
                    Id = v.Id ?? "unknown",
                    Type = v.Type ?? "release",
                    IsInstalled = installed.Contains(v.Id ?? "")
                });
            }

            return result;
        }
        catch
        {
            // nunca deixar crashar a UI
            return new List<MinecraftVersionInfo>();
        }
    }

    // 📦 VERSÕES INSTALADAS (LOCAL)
    public List<string> GetInstalledVersions()
    {
        if (!Directory.Exists(versionsPath))
            return new List<string>();

        var dirs = Directory.GetDirectories(versionsPath);

        var result = new List<string>();

        foreach (var dir in dirs)
        {
            result.Add(Path.GetFileName(dir));
        }

        return result;
    }

    // 📥 CHECK SE INSTALADA
    public bool IsVersionInstalled(string version)
    {
        var path = Path.Combine(versionsPath, version);
        return Directory.Exists(path);
    }

    // ─────────────────────────────
    // MODELS INTERNOS
    // ─────────────────────────────

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