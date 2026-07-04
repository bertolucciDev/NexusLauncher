using NexusLauncher.Minecraft.Java;
using NexusLauncher.Minecraft.Launch;
using NexusLauncher.Minecraft.Versions;
using NexusLauncher.Models;
using NexusLauncher.Services;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;

namespace NexusLauncher.Minecraft;

public class MinecraftService
{
    private readonly JavaManager _javaManager = new();
    private readonly VersionManager _versionManager = new(MinecraftPaths.GamePath);
    private readonly LaunchManager _launchManager = new(MinecraftPaths.GamePath);
    private readonly ProcessService _processService;
    private string _statusMessage = "Pronto";

    public MinecraftService(ProcessService? processService = null)
    {
        _processService = processService ?? LauncherRuntime.Processes;
    }

    public string MinecraftDirectory => MinecraftPaths.Root;
    public string? JavaPath => _javaManager.FindJavaPath();
    public int? CurrentProcessId => _processService.CurrentProcessId;
    public bool IsGameRunning => _processService.IsGameRunning;

    public bool IsJavaReady() => _javaManager.IsJava17OrHigher(JavaPath);
    public bool IsJavaReadyFor(string version)
    {
        var requiredJava = _javaManager.GetRequiredJavaMajor(version);
        var javaPath = _javaManager.FindJavaPath(requiredJava);
        return !string.IsNullOrWhiteSpace(javaPath) && _javaManager.GetMajorVersion(javaPath) >= requiredJava;
    }

    public string GetJavaStatusMessage()
    {
        if (string.IsNullOrWhiteSpace(JavaPath))
            return "Java não encontrado";

        return IsJavaReady() ? $"Java pronto: {JavaPath}" : "Java não atende ao requisito mínimo (17+)";
    }

    public List<string> GetInstalledVersions() => _versionManager.GetInstalledVersions();

    public List<string> GetVersions() => GetInstalledVersions();

    public bool IsVersionInstalled(string version) => _versionManager.IsInstalled(version);

    public async Task<string?> InstallVersionAsync(string version, System.IProgress<DownloadProgressInfo>? progress = null) => await _versionManager.DownloadVersionAsync(version, progress);

    public async Task<bool> EnsureVersionReadyAsync(string version, string username)
        => await EnsureVersionReadyAsync(version, username, LauncherRuntime.Settings.Load());

    public async Task<bool> EnsureVersionReadyAsync(string version, string username, LauncherSettings settings)
    {
        var process = await PrepareAndLaunchAsync(version, username, settings);
        return process is not null;
    }

    public async Task<Process?> PrepareAndLaunchAsync(string version, string username, LauncherSettings settings, System.IProgress<DownloadProgressInfo>? progress = null)
    {
        var requiredJava = _javaManager.GetRequiredJavaMajor(version);
        var javaPath = string.IsNullOrWhiteSpace(settings.JavaPath) ? _javaManager.FindJavaPath(requiredJava) : settings.JavaPath;
        if (string.IsNullOrWhiteSpace(javaPath) || _javaManager.GetMajorVersion(javaPath) < requiredJava)
        {
            _statusMessage = "Java 17+ não encontrado. Instale o Java e tente novamente.";
            return null;
        }

        if (!IsVersionInstalled(version))
        {
            _statusMessage = "Baixando e instalando Minecraft...";
            var installed = await InstallVersionAsync(version, progress);
            if (string.IsNullOrWhiteSpace(installed))
            {
                _statusMessage = "Falha ao instalar a versão selecionada.";
                return null;
            }
        }

        _statusMessage = "Iniciando Minecraft...";
        var process = await _launchManager.LaunchAsync(version, username, javaPath, settings);
        if (process is null)
        {
            _statusMessage = "Falha ao iniciar Minecraft. Verifique os logs em Minecraft/Launch/.";
            return null;
        }

        _processService.Track(process, settings);
        _statusMessage = $"Minecraft iniciado (PID {process.Id})";
        return process;
    }

    public string GetStatusMessage() => _statusMessage;
}
