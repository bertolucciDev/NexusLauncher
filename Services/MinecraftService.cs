using System.Collections.Generic;
using System.Threading.Tasks;

namespace NexusLauncher.Services;

public class MinecraftService
{
    private readonly NexusLauncher.Minecraft.MinecraftService _innerService = new();

    public string? JavaPath => _innerService.JavaPath;

    public bool IsJavaReady() => _innerService.IsJavaReady();

    public List<string> GetInstalledVersions() => _innerService.GetInstalledVersions();

    public List<string> GetVersions() => _innerService.GetVersions();

    public bool IsVersionInstalled(string version) => _innerService.IsVersionInstalled(version);

    public Task<string?> InstallVersionAsync(string version) => _innerService.InstallVersionAsync(version);

    public Task<bool> EnsureVersionReadyAsync(string version, string username) => _innerService.EnsureVersionReadyAsync(version, username);

    public string GetStatusMessage() => _innerService.GetStatusMessage();
}