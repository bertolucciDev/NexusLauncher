using CmlLib.Core;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

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

    public async Task<string?> DownloadVersionAsync(string version)
    {
        if (string.IsNullOrWhiteSpace(version))
            return null;

        await _launcher.InstallAsync(version);
        return IsInstalled(version) ? version : null;
    }
}
