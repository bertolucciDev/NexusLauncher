using NexusLauncher.Models;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;

namespace NexusLauncher.Services;

public class MinecraftService
{
    private readonly NexusLauncher.Minecraft.MinecraftService _innerService = new();

    public string? JavaPath => _innerService.JavaPath;

    public string MinecraftDirectory => _innerService.MinecraftDirectory;

    public int? CurrentProcessId => _innerService.CurrentProcessId;

    public bool IsGameRunning => _innerService.IsGameRunning;

    public bool IsJavaReady() => _innerService.IsJavaReady();

    public List<string> GetInstalledVersions() => _innerService.GetInstalledVersions();

    public List<string> GetVersions() => _innerService.GetVersions();

    public bool IsVersionInstalled(string version) => _innerService.IsVersionInstalled(version);

    public Task<string?> InstallVersionAsync(string version) => _innerService.InstallVersionAsync(version);

    public Task<bool> EnsureVersionReadyAsync(string version, string username) => _innerService.EnsureVersionReadyAsync(version, username);

    public Task<bool> EnsureVersionReadyAsync(string version, string username, LauncherSettings settings) => _innerService.EnsureVersionReadyAsync(version, username, settings);

    public Task<Process?> PrepareAndLaunchAsync(string version, string username, LauncherSettings settings) => _innerService.PrepareAndLaunchAsync(version, username, settings);

    public string GetStatusMessage() => _innerService.GetStatusMessage();
}