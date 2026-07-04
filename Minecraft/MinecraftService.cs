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

    public string GetJavaStatusMessage()
    {
        if (string.IsNullOrWhiteSpace(JavaPath))
            return "Java não encontrado";

        return IsJavaReady() ? $"Java pronto: {JavaPath}" : "Java não atende ao requisito mínimo (17+)";
    }

    public List<string> GetInstalledVersions() => _versionManager.GetInstalledVersions();

    public List<string> GetVersions() => GetInstalledVersions();

    public bool IsVersionInstalled(string version) => _versionManager.IsInstalled(version);

    public async Task<string?> InstallVersionAsync(string version) => await _versionManager.DownloadVersionAsync(version);

    public async Task<bool> EnsureVersionReadyAsync(string version, string username)
        => await EnsureVersionReadyAsync(version, username, LauncherRuntime.Settings.Load());

    public async Task<bool> EnsureVersionReadyAsync(string version, string username, LauncherSettings settings)
    {
        var process = await PrepareAndLaunchAsync(version, username, settings);
        return process is not null;
    }

    public async Task<Process?> PrepareAndLaunchAsync(string version, string username, LauncherSettings settings)
    {
        if (!IsJavaReady() || string.IsNullOrWhiteSpace(JavaPath))
        {
            _statusMessage = "Java 17+ não encontrado. Instale o Java e tente novamente.";
            return null;
        }

        if (!IsVersionInstalled(version))
        {
            _statusMessage = "Baixando e instalando Minecraft...";
            var installed = await InstallVersionAsync(version);
            if (string.IsNullOrWhiteSpace(installed))
            {
                _statusMessage = "Falha ao instalar a versão selecionada.";
                return null;
            }
        }

        _statusMessage = "Iniciando Minecraft...";
        var process = await _launchManager.LaunchAsync(version, username, JavaPath, settings);
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
