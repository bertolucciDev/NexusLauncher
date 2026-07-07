using NexusLauncher.Minecraft.Java;
using NexusLauncher.Minecraft.Launch;
using NexusLauncher.Minecraft.Versions;
using NexusLauncher.Models;
using NexusLauncher.Services;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace NexusLauncher.Minecraft;

public class MinecraftService
{
    private readonly JavaManager _javaManager = new();
    private readonly VersionManager _versionManager = new(NexusLauncher.Minecraft.MinecraftPaths.GamePath);
    private readonly LaunchManager _launchManager = new(NexusLauncher.Minecraft.MinecraftPaths.GamePath);
    private readonly ProcessService _processService;
    private readonly HttpClient _http = new();
    private readonly Services.NexuSkinService _nexusSkin = new();
    private string _statusMessage = "Pronto";

    public MinecraftService(ProcessService? processService = null)
    {
        _processService = processService ?? LauncherRuntime.Processes;
    }

    public string MinecraftDirectory => NexusLauncher.Minecraft.MinecraftPaths.Root;
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

    public async Task<Process?> PrepareAndLaunchAsync(string version, string username, LauncherSettings settings, System.IProgress<DownloadProgressInfo>? progress = null, string? instancePath = null)
    {
        var requiredJava = _javaManager.GetRequiredJavaMajor(version);

        string? javaPath;
        if (!string.IsNullOrWhiteSpace(instancePath))
            javaPath = await _javaManager.EnsureJavaAsync(requiredJava);
        else
            javaPath = string.IsNullOrWhiteSpace(settings.JavaPath) ? _javaManager.FindJavaPath(requiredJava) : settings.JavaPath;

        if (string.IsNullOrWhiteSpace(javaPath) || _javaManager.GetMajorVersion(javaPath) < requiredJava)
        {
            _statusMessage = $"Java {requiredJava}+ não encontrado. Instale o Java e tente novamente.";
            return null;
        }

        var launchVersion = version;
        if (!string.IsNullOrWhiteSpace(instancePath))
        {
            var loaderVersion = await InstallLoaderIfNeededAsync(instancePath, version, progress);
            if (loaderVersion is not null)
                launchVersion = loaderVersion;
        }

        _statusMessage = "Baixando e instalando Minecraft...";
        await InstallVersionAsync(launchVersion, progress);

        _statusMessage = "Iniciando Minecraft...";
        var process = await _launchManager.LaunchAsync(launchVersion, username, javaPath, settings, instancePath);
        if (process is null)
        {
            _statusMessage = "Falha ao iniciar Minecraft. Verifique os logs internos do NexusLauncher.";
            return null;
        }

        _processService.Track(process, settings);
        _statusMessage = $"Minecraft iniciado (PID {process.Id})";
        return process;
    }

    private async Task EnsureAgentForUserAsync(string username)
    {
        try
        {
            await _nexusSkin.EnsureAgentAsync();
            await _nexusSkin.PrepareForUserAsync(username);
        }
        catch (Exception ex)
        {
            _statusMessage = "NexuSkin: " + ex.Message;
        }
    }

    private async Task PrestoreFriendsSkinsAsync(System.IProgress<DownloadProgressInfo>? progress)
    {
        try
        {
            var settings = LauncherRuntime.Settings.Load();
            var friends = settings.FriendUsernames;
            if (friends is null || friends.Count == 0) return;
            progress?.Report(new DownloadProgressInfo
            {
                CurrentFile = $"NexuSkin: pré-baixando {friends.Count} skins de amigos..."
            });
            await _nexusSkin.PrefetchManyAsync(friends);
        }
        catch (Exception ex) { System.Console.WriteLine($"[MinecraftService] PrestoreFriendsSkins: {ex.Message}"); }
    }

    private async Task<string?> InstallLoaderIfNeededAsync(string instancePath, string mcVersion, System.IProgress<DownloadProgressInfo>? progress)
    {
        var jsonPath = Path.Combine(instancePath, "instance.json");
        if (!File.Exists(jsonPath)) return null;

        MinecraftInstance instance;
        try { instance = JsonSerializer.Deserialize<MinecraftInstance>(File.ReadAllText(jsonPath))!; }
        catch (Exception ex) { System.Console.WriteLine($"[MinecraftService] Loader json parse: {ex.Message}"); return null; }
        if (instance is null) return null;

        var loader = instance.Loader?.Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(loader) || loader == "vanilla") return null;

        progress?.Report(new DownloadProgressInfo { CurrentFile = $"Preparando {loader} para {mcVersion}..." });

        return loader switch
        {
            "fabric" => await InstallFabricAsync(mcVersion, progress),
            "quilt" => await InstallQuiltAsync(mcVersion, progress),
            "forge" => await InstallForgeAsync(mcVersion, progress),
            "neoforge" => await InstallNeoForgeAsync(mcVersion, progress),
            _ => null
        };
    }

    private async Task<string?> InstallFabricAsync(string mcVersion, System.IProgress<DownloadProgressInfo>? progress)
    {
        var versionsDir = NexusLauncher.Minecraft.MinecraftPaths.Versions;
        try
        {
            var loaderJson = await _http.GetStringAsync($"https://meta.fabricmc.net/v2/versions/loader/{mcVersion}");
            using var doc = JsonDocument.Parse(loaderJson);
            if (doc.RootElement.ValueKind != JsonValueKind.Array || doc.RootElement.GetArrayLength() == 0) return null;
            var loaderVersion = doc.RootElement[0].GetProperty("loader").GetProperty("version").GetString();
            if (string.IsNullOrWhiteSpace(loaderVersion)) return null;
            return await DownloadProfileAsync(versionsDir, $"fabric-loader-{loaderVersion}-{mcVersion}",
                $"https://meta.fabricmc.net/v2/versions/loader/{mcVersion}/{loaderVersion}/profile/json", progress);
        }
        catch (Exception ex) { System.Console.WriteLine($"[MinecraftService] InstallFabric: {ex.Message}"); return null; }
    }

    private async Task<string?> InstallQuiltAsync(string mcVersion, System.IProgress<DownloadProgressInfo>? progress)
    {
        var versionsDir = NexusLauncher.Minecraft.MinecraftPaths.Versions;
        try
        {
            var loaderJson = await _http.GetStringAsync($"https://meta.quiltmc.org/v3/versions/loader/{mcVersion}");
            using var doc = JsonDocument.Parse(loaderJson);
            if (doc.RootElement.ValueKind != JsonValueKind.Array || doc.RootElement.GetArrayLength() == 0) return null;
            var loaderVersion = doc.RootElement[0].GetProperty("loader").GetProperty("version").GetString();
            if (string.IsNullOrWhiteSpace(loaderVersion)) return null;
            return await DownloadProfileAsync(versionsDir, $"quilt-loader-{loaderVersion}-{mcVersion}",
                $"https://meta.quiltmc.org/v3/versions/loader/{mcVersion}/{loaderVersion}/profile/json", progress);
        }
        catch (Exception ex) { System.Console.WriteLine($"[MinecraftService] InstallQuilt: {ex.Message}"); return null; }
    }

    private async Task<string?> InstallForgeAsync(string mcVersion, System.IProgress<DownloadProgressInfo>? progress)
    {
        var versionsDir = NexusLauncher.Minecraft.MinecraftPaths.Versions;
        try
        {
            var promosJson = await _http.GetStringAsync("https://files.minecraftforge.net/net/minecraftforge/forge/promotions_slim.json");
            using var doc = JsonDocument.Parse(promosJson);
            var promos = doc.RootElement.GetProperty("promos");
            var forgeVersion = TryGetString(promos, $"{mcVersion}-recommended") ?? TryGetString(promos, $"{mcVersion}-latest");
            if (string.IsNullOrWhiteSpace(forgeVersion)) return null;
            return await InstallFromJarAsync(versionsDir,
                $"forge-{mcVersion}-{forgeVersion}", "forge", progress);
        }
        catch (Exception ex) { System.Console.WriteLine($"[MinecraftService] InstallForge: {ex.Message}"); return null; }
    }

    private async Task<string?> InstallNeoForgeAsync(string mcVersion, System.IProgress<DownloadProgressInfo>? progress)
    {
        var versionsDir = NexusLauncher.Minecraft.MinecraftPaths.Versions;
        try
        {
            var prefix = GetNeoForgePrefix(mcVersion);
            if (prefix is null) return null;

            var metadataXml = await _http.GetStringAsync("https://maven.neoforged.net/releases/net/neoforged/neoforge/maven-metadata.xml");
            var versions = ParseMavenVersions(metadataXml);
            var matched = versions.Where(v => v.StartsWith(prefix)).OrderByDescending(v => v).FirstOrDefault();
            if (matched is null) return null;

            return await InstallFromJarAsync(versionsDir,
                $"neoforge-{matched}", "neoforge", progress);
        }
        catch (Exception ex) { System.Console.WriteLine($"[MinecraftService] InstallNeoForge: {ex.Message}"); return null; }
    }

    private static string? GetNeoForgePrefix(string mcVersion)
    {
        var parts = mcVersion.Split('.');
        if (parts.Length < 2) return null;
        if (parts[0] != "1") return null;
        return $"{parts[1]}.{string.Join(".", parts.Skip(2).Take(2))}";
    }

    private static List<string> ParseMavenVersions(string xml)
    {
        var versions = new List<string>();
        var versionTag = "<version>";
        var endTag = "</version>";
        int idx = 0;
        while ((idx = xml.IndexOf(versionTag, idx, StringComparison.Ordinal)) >= 0)
        {
            idx += versionTag.Length;
            var end = xml.IndexOf(endTag, idx, StringComparison.Ordinal);
            if (end < 0) break;
            versions.Add(xml[idx..end]);
            idx = end + endTag.Length;
        }
        return versions;
    }

    private async Task<string?> InstallFromJarAsync(string versionsDir, string artifactPrefix, string loaderName, System.IProgress<DownloadProgressInfo>? progress)
    {
        var repo = loaderName == "neoforge"
            ? "https://maven.neoforged.net/releases/net/neoforged/neoforge"
            : "https://maven.minecraftforge.net/net/minecraftforge/forge";

        var versionPart = artifactPrefix[(loaderName.Length + 1)..];
        var installerUrl = $"{repo}/{versionPart}/{artifactPrefix}-installer.jar";

        var tempDir = Path.Combine(Path.GetTempPath(), "nx-installer", Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);
        var jarPath = Path.Combine(tempDir, "installer.jar");

        try
        {
            progress?.Report(new DownloadProgressInfo { CurrentFile = $"Baixando {loaderName} {versionPart}..." });

            using (var response = await _http.GetAsync(installerUrl, HttpCompletionOption.ResponseHeadersRead))
            {
                response.EnsureSuccessStatusCode();
                using var fs = new FileStream(jarPath, FileMode.Create);
                await response.Content.CopyToAsync(fs);
            }

            progress?.Report(new DownloadProgressInfo { CurrentFile = $"Instalando {loaderName} {versionPart}..." });

            using var archive = ZipFile.OpenRead(jarPath);
            var versionEntry = archive.GetEntry("version.json");
            if (versionEntry is null) return null;

            using var reader = new StreamReader(versionEntry.Open());
            var versionJson = await reader.ReadToEndAsync();

            using var vDoc = JsonDocument.Parse(versionJson);
            var profileId = vDoc.RootElement.GetProperty("id").GetString();
            if (string.IsNullOrWhiteSpace(profileId)) return null;

            var profileDir = Path.Combine(versionsDir, profileId);
            var profileFile = Path.Combine(profileDir, $"{profileId}.json");
            if (File.Exists(profileFile)) return profileId;

            Directory.CreateDirectory(profileDir);
            await File.WriteAllTextAsync(profileFile, versionJson);
            return profileId;
        }
        finally
        {
            try { Directory.Delete(tempDir, true); } catch (Exception) { }
        }
    }

    private static string? TryGetString(JsonElement element, string key)
    {
        if (element.TryGetProperty(key, out var prop) && prop.ValueKind == JsonValueKind.String)
            return prop.GetString();
        return null;
    }

    private async Task<string?> DownloadProfileAsync(string versionsDir, string profileId, string url, System.IProgress<DownloadProgressInfo>? progress)
    {
        var profileDir = Path.Combine(versionsDir, profileId);
        var profileFile = Path.Combine(profileDir, $"{profileId}.json");
        if (File.Exists(profileFile)) return profileId;

        progress?.Report(new DownloadProgressInfo { CurrentFile = $"Baixando {profileId}..." });
        var json = await _http.GetStringAsync(url);
        Directory.CreateDirectory(profileDir);
        await File.WriteAllTextAsync(profileFile, json);
        return profileId;
    }

    public string GetStatusMessage() => _statusMessage;

    private void LogStep(string version, string message)
    {
        try
        {
            var logPath = Path.Combine(NexusLauncher.Minecraft.MinecraftPaths.LaunchLogs, $"{version}.log");
            File.AppendAllText(logPath, $"[{DateTimeOffset.Now:O}] [LAUNCH_STEP] {message}{Environment.NewLine}");
        }
        catch { /* silent */ }
    }
}
