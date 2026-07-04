using CmlLib.Core;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using NexusLauncher.Models;
using System;

namespace NexusLauncher.Minecraft.Versions;

public class VersionManager
{
    private readonly MinecraftLauncher _launcher;
    private readonly MinecraftPath _path;

    public VersionManager(MinecraftPath? path = null)
    {
        _path = path ?? MinecraftPaths.GamePath;
        _launcher = new MinecraftLauncher(_path);
    }

    public List<string> GetInstalledVersions()
    {
        if (!Directory.Exists(_path.Versions))
            return new List<string>();

        return Directory.GetDirectories(_path.Versions)
            .Select(Path.GetFileName)
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .OrderByDescending(name => name)
            .ToList()!;
    }

    public bool IsInstalled(string version)
    {
        if (string.IsNullOrWhiteSpace(version))
            return false;

        return File.Exists(_path.GetVersionJsonPath(version)) && File.Exists(_path.GetVersionJarPath(version));
    }

    public async Task<string?> DownloadVersionAsync(string version, IProgress<DownloadProgressInfo>? progress = null)
    {
        if (string.IsNullOrWhiteSpace(version))
            return null;

        progress?.Report(new DownloadProgressInfo { State = "Baixando", CurrentFile = version, BytesDownloaded = 0, TotalBytes = 100 });
        await _launcher.InstallAsync(version);
        progress?.Report(new DownloadProgressInfo { State = IsInstalled(version) ? "Concluído" : "Falhou", CurrentFile = version, BytesDownloaded = IsInstalled(version) ? 100 : 0, TotalBytes = 100 });
        return IsInstalled(version) ? version : null;
    }
}
