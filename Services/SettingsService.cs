using NexusLauncher.Models;
using NexusLauncher.Minecraft;
using System;
using System.IO;
using System.Text.Json;

namespace NexusLauncher.Services;

public class SettingsService
{
    private readonly string _path;
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    public SettingsService(string? path = null)
    {
        _path = path ?? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Storage", "LocalConfig.json");
        Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
    }

    public LauncherSettings Load()
    {
        if (!File.Exists(_path))
            return new LauncherSettings();

        try
        {
            var settings = JsonSerializer.Deserialize<LauncherSettings>(File.ReadAllText(_path), new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            return Normalize(settings ?? new LauncherSettings());
        }
        catch
        {
            return new LauncherSettings();
        }
    }

    public void Save(LauncherSettings settings)
    {
        File.WriteAllText(_path, JsonSerializer.Serialize(Normalize(settings), JsonOptions));
    }

    public int GetMaxRamGb()
    {
        var totalBytes = GC.GetGCMemoryInfo().TotalAvailableMemoryBytes;
        if (totalBytes <= 0)
            totalBytes = 8L * 1024 * 1024 * 1024;

        var totalGb = Math.Max(2, (int)(totalBytes / 1024 / 1024 / 1024));
        return Math.Max(2, (int)Math.Floor(totalGb * 0.75));
    }

    private LauncherSettings Normalize(LauncherSettings settings)
    {
        var maxRam = GetMaxRamGb();
        settings.Nickname = string.IsNullOrWhiteSpace(settings.Nickname) ? "Player" : settings.Nickname.Trim();
        settings.AllocatedRamGb = Math.Clamp(settings.AllocatedRamGb, 2, maxRam);
        settings.Resolution = string.IsNullOrWhiteSpace(settings.Resolution) ? "1280x720" : settings.Resolution;
        settings.MinecraftDirectory = string.IsNullOrWhiteSpace(settings.MinecraftDirectory) ? MinecraftPaths.GetOfficialDefaultRoot() : Environment.ExpandEnvironmentVariables(settings.MinecraftDirectory.Trim());
        settings.FavoriteVersions ??= new System.Collections.Generic.List<string>();
        settings.LastPlayedVersion = settings.LastPlayedVersion ?? string.Empty;
        settings.LastVersionCategory = string.IsNullOrWhiteSpace(settings.LastVersionCategory) ? "Releases" : settings.LastVersionCategory;
        settings.JavaPath = settings.JavaPath ?? string.Empty;
        settings.Theme = string.IsNullOrWhiteSpace(settings.Theme) ? "Dark" : settings.Theme;
        return settings;
    }
}
