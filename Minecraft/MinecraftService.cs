using NexusLauncher.Minecraft.Java;
using NexusLauncher.Minecraft.Launch;
using NexusLauncher.Minecraft.Versions;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NexusLauncher.Minecraft;

public class MinecraftService
{
    private readonly JavaManager _javaManager = new();
    private readonly VersionManager _versionManager = new(MinecraftPaths.GamePath);
    private readonly LaunchManager _launchManager = new(MinecraftPaths.GamePath);
    private string _statusMessage = "Pronto";

    public string MinecraftDirectory => MinecraftPaths.Root;
    public string? JavaPath => _javaManager.FindJavaPath();

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
    {
        if (!IsJavaReady() || string.IsNullOrWhiteSpace(JavaPath))
        {
            _statusMessage = "Java 17+ não encontrado. Instale o Java e tente novamente.";
            return false;
        }

        if (!IsVersionInstalled(version))
        {
            _statusMessage = "Baixando e instalando Minecraft...";
            var installed = await InstallVersionAsync(version);
            if (string.IsNullOrWhiteSpace(installed))
            {
                _statusMessage = "Falha ao instalar a versão selecionada.";
                return false;
            }
        }

        _statusMessage = "Iniciando Minecraft...";
        var launched = await _launchManager.LaunchAsync(version, username, JavaPath);
        _statusMessage = launched ? "Minecraft iniciado" : "Falha ao iniciar Minecraft. Verifique os logs em Minecraft/Launch/.";
        return launched;
    }

    public string GetStatusMessage() => _statusMessage;
}
