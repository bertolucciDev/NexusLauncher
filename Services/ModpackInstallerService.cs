using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using NexusLauncher.Models;
using NexusLauncher.Services;

namespace NexusLauncher.Services;

public class ModpackInstallerService
{
    private readonly ModrinthService _modrinthService;
    private readonly CurseForgeService _curseForgeService;
    private readonly DownloadService _downloadService;
    private readonly SettingsService _settingsService;
    private readonly InstanceService _instanceService;

    public ModpackInstallerService(ModrinthService modrinthService, CurseForgeService curseForgeService, DownloadService downloadService, SettingsService settingsService, InstanceService instanceService)
    {
        _modrinthService = modrinthService;
        _curseForgeService = curseForgeService;
        _downloadService = downloadService;
        _settingsService = settingsService;
        _instanceService = instanceService;
    }

    public async Task InstallModpackAsync(ProjectItem project, string instanceName, IProgress<DownloadProgressInfo>? progress = null, CancellationToken ct = default)
    {
        if (project.Source == "CurseForge")
        {
            await InstallCurseForgeModpackAsync(project, instanceName, progress, ct);
            return;
        }

        await InstallModrinthModpackAsync(project, instanceName, progress, ct);
    }

    private async Task InstallModrinthModpackAsync(ProjectItem project, string instanceName, IProgress<DownloadProgressInfo>? progress = null, CancellationToken ct = default)
    {
        var versions = await _modrinthService.GetVersionsAsync(project.ProjectId, ct);
        if (versions.Count == 0) throw new Exception("Nenhuma versão encontrada para este projeto.");

        var latestVersion = versions.Find(v => v.Files.Exists(f => f.Primary)) ?? versions[0];
        var primaryFile = latestVersion.Files.Find(f => f.Primary) ?? latestVersion.Files[0];

        var mcVersion = latestVersion.GameVersions.Count > 0 ? latestVersion.GameVersions[0] : "1.20.1";
        var loader = latestVersion.Loaders.Count > 0 ? latestVersion.Loaders[0] : "vanilla";

        var instance = _instanceService.Create(instanceName, mcVersion, loader, "latest");
        string tempPackPath = Path.Combine(Path.GetTempPath(), $"{project.ProjectId}_{latestVersion.Id}.mrpack");

        progress?.Report(new DownloadProgressInfo { State = "Baixando .mrpack", CurrentFile = project.Title, BytesDownloaded = 0, TotalBytes = 100 });

        await _modrinthService.DownloadFileAsync(primaryFile.Url, tempPackPath, null, ct);

        using (ZipArchive archive = ZipFile.OpenRead(tempPackPath))
        {
            var indexEntry = archive.GetEntry("modrinth.index.json");
            if (indexEntry == null) throw new Exception("mrpack inválido: modrinth.index.json não encontrado.");

            using var stream = indexEntry.Open();
            var index = await JsonSerializer.DeserializeAsync<ModrinthPackIndex>(stream, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                NumberHandling = JsonNumberHandling.AllowReadingFromString
            }, ct);
            if (index?.Files == null) throw new Exception("Falha ao ler modrinth.index.json.");

            for (int i = 0; i < index.Files.Count; i++)
            {
                var fileEntry = index.Files[i];
                if (fileEntry.Downloads == null || fileEntry.Downloads.Count == 0) continue;

                string destPath = Path.Combine(instance.Path, fileEntry.Path);
                var dir = Path.GetDirectoryName(destPath);
                if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);

                string fileName = Path.GetFileName(fileEntry.Path);
                progress?.Report(new DownloadProgressInfo
                {
                    State = $"Arquivo {i + 1}/{index.Files.Count}",
                    CurrentFile = fileName,
                    BytesDownloaded = i * 100L / Math.Max(1, index.Files.Count),
                    TotalBytes = 100
                });

                int attempts = 0;
                bool ok = false;
                while (attempts < 3 && !ok)
                {
                    try
                    {
                        attempts++;
                        await _modrinthService.DownloadFileAsync(fileEntry.Downloads[0], destPath, null, ct);
                        ok = true;
                    }
                    catch (Exception) when (attempts < 3)
                    {
                        await Task.Delay(2000 * attempts, ct);
                    }
                }
                if (!ok) throw new Exception($"Falha ao baixar {fileName} após 3 tentativas.");
            }

            foreach (var entry in archive.Entries)
            {
                if (entry.FullName == "modrinth.index.json") continue;
                if (!entry.FullName.StartsWith("overrides/", StringComparison.OrdinalIgnoreCase) &&
                    !entry.FullName.StartsWith("overrides\\", StringComparison.OrdinalIgnoreCase)) continue;

                string relativePath = entry.FullName.Substring("overrides/".Length);
                if (string.IsNullOrWhiteSpace(relativePath)) continue;

                string destPath = Path.Combine(instance.Path, relativePath);
                var dir = Path.GetDirectoryName(destPath);
                if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);

                if (!string.IsNullOrEmpty(entry.Name))
                    entry.ExtractToFile(destPath, true);
            }
        }

        try { File.Delete(tempPackPath); } catch { }
        progress?.Report(new DownloadProgressInfo { State = "Concluído", CurrentFile = "Instalação finalizada!", BytesDownloaded = 100, TotalBytes = 100 });
    }

    private async Task InstallCurseForgeModpackAsync(ProjectItem project, string instanceName, IProgress<DownloadProgressInfo>? progress = null, CancellationToken ct = default)
    {
        var cfFile = await _curseForgeService.GetLatestFileAsync(project.CurseForgeId, ct);
        if (cfFile is null)
            throw new Exception("Nenhum arquivo encontrado para este modpack no CurseForge.");

        var tempDir = Path.Combine(Path.GetTempPath(), $"cf_modpack_{project.CurseForgeId}");
        if (Directory.Exists(tempDir)) try { Directory.Delete(tempDir, true); } catch { }
        Directory.CreateDirectory(tempDir);

        string downloadUrl;
        if (!string.IsNullOrWhiteSpace(cfFile.DownloadUrl))
        {
            downloadUrl = cfFile.DownloadUrl;
        }
        else
        {
            var redirectUrl = await _curseForgeService.GetDownloadUrlAsync(project.CurseForgeId, cfFile.Id, ct);
            if (redirectUrl is null)
                throw new Exception("URL de download nao disponivel para este modpack.");
            downloadUrl = redirectUrl;
        }

        string tempZipPath = Path.Combine(tempDir, cfFile.FileName);
        progress?.Report(new DownloadProgressInfo { State = "Baixando modpack", CurrentFile = project.Title, BytesDownloaded = 0, TotalBytes = 100 });
        await _downloadService.DownloadFileAsync(downloadUrl, tempZipPath, null, ct);

        string mcVersion;
        string loader;
        List<CurseForgeManifestFile> modFiles;
        MinecraftInstance instance;

        using (ZipArchive archive = ZipFile.OpenRead(tempZipPath))
        {
            var manifestEntry = archive.GetEntry("manifest.json");
            if (manifestEntry == null)
                throw new Exception("Modpack CurseForge inválido: manifest.json não encontrado.");

            using var stream = manifestEntry.Open();
            using var reader = new StreamReader(stream);
            var manifestJson = await reader.ReadToEndAsync(ct);

            var manifest = JsonSerializer.Deserialize<CurseForgeManifest>(manifestJson, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (manifest?.Minecraft?.Version is null)
                throw new Exception("manifest.json inválido: minecraft.version não encontrado.");

            mcVersion = manifest.Minecraft.Version;
            var rawLoader = manifest.Minecraft.ModLoaders?.FirstOrDefault()?.Id ?? "forge";
            loader = rawLoader.Contains('-') ? rawLoader.Split('-')[0] : rawLoader;
            modFiles = manifest.Files ?? new List<CurseForgeManifestFile>();

            instance = _instanceService.Create(instanceName, mcVersion, loader, "latest");

            foreach (var entry in archive.Entries)
            {
                if (entry.FullName == "manifest.json") continue;
                if (!entry.FullName.StartsWith("overrides/", StringComparison.OrdinalIgnoreCase) &&
                    !entry.FullName.StartsWith("overrides\\", StringComparison.OrdinalIgnoreCase)) continue;

                string relativePath = entry.FullName.Substring("overrides/".Length);
                if (string.IsNullOrWhiteSpace(relativePath)) continue;

                string destPath = Path.Combine(instance.Path, relativePath);
                var dir = Path.GetDirectoryName(destPath);
                if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);

                if (!string.IsNullOrEmpty(entry.Name))
                    entry.ExtractToFile(destPath, true);
            }
        }

        try { File.Delete(tempZipPath); } catch { }

        if (modFiles.Count > 0)
        {
            progress?.Report(new DownloadProgressInfo { State = "Baixando mods", CurrentFile = "Preparando...", BytesDownloaded = 0, TotalBytes = modFiles.Count });

            for (int i = 0; i < modFiles.Count; i++)
            {
                var mf = modFiles[i];
                ct.ThrowIfCancellationRequested();

                progress?.Report(new DownloadProgressInfo
                {
                    State = $"Mod {i + 1}/{modFiles.Count}",
                    CurrentFile = $"Mod {mf.ProjectID}",
                    BytesDownloaded = i,
                    TotalBytes = modFiles.Count
                });

                int attempts = 0;
                bool ok = false;
                while (attempts < 3 && !ok)
                {
                    try
                    {
                        attempts++;
                        var file = await _curseForgeService.GetLatestFileAsync(mf.ProjectID, ct);
                        if (file is null) { ok = true; continue; }

                        string modUrl;
                        if (!string.IsNullOrWhiteSpace(file.DownloadUrl))
                        {
                            modUrl = file.DownloadUrl;
                        }
                        else
                        {
                            modUrl = await _curseForgeService.GetDownloadUrlAsync(mf.ProjectID, file.Id, ct);
                            if (modUrl is null) { ok = true; continue; }
                        }

                        string destPath = Path.Combine(instance.Path, "mods", file.FileName);
                        var dir = Path.GetDirectoryName(destPath);
                        if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);

                        await _downloadService.DownloadFileAsync(modUrl, destPath, null, ct);
                        ok = true;
                    }
                    catch (Exception) when (attempts < 3)
                    {
                        await Task.Delay(2000 * attempts, ct);
                    }
                }
            }
        }

        progress?.Report(new DownloadProgressInfo { State = "Concluído", CurrentFile = "Instalação finalizada!", BytesDownloaded = 100, TotalBytes = 100 });
    }

    private sealed class CurseForgeManifest
    {
        public CurseForgeManifestMinecraft? Minecraft { get; set; }
        public List<CurseForgeManifestFile>? Files { get; set; }
    }

    private sealed class CurseForgeManifestMinecraft
    {
        public string? Version { get; set; }
        public List<CurseForgeManifestModLoader>? ModLoaders { get; set; }
    }

    private sealed class CurseForgeManifestModLoader
    {
        public string? Id { get; set; }
        public bool Primary { get; set; }
    }

    private sealed class CurseForgeManifestFile
    {
        public int ProjectID { get; set; }
        public int FileID { get; set; }
        public bool Required { get; set; }
    }

    private sealed class ModrinthPackIndex
    {
        [JsonPropertyName("files")]
        public List<ModrinthPackFile> Files { get; set; } = new();

        [JsonPropertyName("dependencies")]
        public Dictionary<string, string>? Dependencies { get; set; }
    }

    private sealed class ModrinthPackFile
    {
        [JsonPropertyName("path")]
        public string Path { get; set; } = string.Empty;

        [JsonPropertyName("downloads")]
        public List<string>? Downloads { get; set; }

        [JsonPropertyName("fileSize")]
        public long FileSize { get; set; }
    }
}