using CmlLib.Core;
using CmlLib.Core.Auth;
using CmlLib.Core.ProcessBuilder;
using NexusLauncher.Models;
using NexusLauncher.Services;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace NexusLauncher.Minecraft.Launch;

public class LaunchManager
{
    private readonly MinecraftLauncher _launcher;
    private readonly NexuSkinService _skinService = new();

    public LaunchManager(MinecraftPath? path = null)
    {
        _launcher = new MinecraftLauncher(path ?? MinecraftPaths.GamePath);
    }

    public async Task<Process?> LaunchAsync(string version, string username, string javaPath, LauncherSettings settings, string? gameDirectory = null)
    {
        try
        {
            var safeUserName = string.IsNullOrWhiteSpace(username) ? "Player" : username.Trim();
            var (width, height) = ParseResolution(settings.Resolution);

            // Log imediato para garantir que sabemos que a função foi chamada
            AppendLog(version, "=== Nova tentativa de Launch ===");
            AppendLog(version, $"Username: {safeUserName}");
            AppendLog(version, $"Java Path: {javaPath}");

            var offlineMode = IsOfflineSession(safeUserName);
            if (offlineMode)
                await _skinService.PrepareForUserAsync(safeUserName);

            var option = new MLaunchOption
            {
                Session = MSession.CreateOfflineSession(safeUserName),
                JavaPath = javaPath,
                MaximumRamMb = settings.AllocatedRamGb * 1024,
                ScreenWidth = width,
                ScreenHeight = height,
                FullScreen = settings.Fullscreen
            };
            if (!string.IsNullOrWhiteSpace(gameDirectory))
            {
                option.ExtraGameArguments = new[] { new MArgument(new[] { "--gameDir", gameDirectory }) };
            }

            // NEXUSKIN: Injeção segura do agente de skins
            var agentJvmArg = "";
            if (offlineMode)
            {
                var agentFile = new FileInfo(_skinService.AgentJarPath);
                if (agentFile.Exists && agentFile.Length > 100)
                {
                    agentJvmArg = $"-javaagent:{agentFile.FullName}={safeUserName}";
                    AppendLog(version, $"NexuSkin: Agent preparado: {agentJvmArg}");
                }
            }

            var process = await _launcher.BuildProcessAsync(version, option);
            
            // Injeta o agent DIRETAMENTE nos argumentos do processo, 
            // sem usar o parser do CmlLib que pode quebrar a string
            if (!string.IsNullOrEmpty(agentJvmArg))
            {
                process.StartInfo.Arguments = agentJvmArg + " " + process.StartInfo.Arguments;
                AppendLog(version, "NexuSkin: Agent prefixado aos argumentos da JVM.");
            }
            
            AppendLog(version, "Sessão: " + safeUserName);
            AppendLog(version, "Java Path: " + javaPath);
            AppendLog(version, "Cmd Line: " + process.StartInfo.Arguments);

            process.EnableRaisingEvents = true;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.CreateNoWindow = false;
            process.StartInfo.WorkingDirectory = MinecraftPaths.Root;
            process.StartInfo.WindowStyle = ProcessWindowStyle.Normal;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;
            process.OutputDataReceived += (_, args) => AppendLog(version, args.Data);
            process.ErrorDataReceived += (_, args) => AppendLog(version, args.Data);

            if (!process.Start())
            {
                AppendLog(version, "Erro: process.Start() retornou false.");
                return null;
            }

            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            AppendLog(version, $"Minecraft process started. PID={process.Id}");
            return process;
        }
        catch (Exception ex)
        {
            AppendLog(version, "CRASH NO LAUNCHER: " + ex.ToString());
            return null;
        }
    }

    private static bool IsOfflineSession(string username)
    {
        if (string.IsNullOrWhiteSpace(username)) return false;
        if (username.Length < 3 || username.Length > 16) return false;
        foreach (var c in username)
            if (!char.IsLetterOrDigit(c) && c != '_') return false;
        return true;
    }

    private static string Quote(string s)
        => s.Contains(' ') ? "\"" + s + "\"" : s;

    private static (int Width, int Height) ParseResolution(string? resolution)
    {
        var parts = resolution?.Split('x', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        if (parts?.Length == 2 && int.TryParse(parts[0], out var width) && int.TryParse(parts[1], out var height))
            return (width, height);

        return (1280, 720);
    }

    private static void AppendLog(string version, string? message)
    {
        if (string.IsNullOrWhiteSpace(message)) return;
        Directory.CreateDirectory(MinecraftPaths.LaunchLogs);
        var safeName = string.Join("_", version.Split(Path.GetInvalidFileNameChars()));
        var logFile = Path.Combine(MinecraftPaths.LaunchLogs, $"{safeName}.log");
        File.AppendAllText(logFile, $"[{DateTimeOffset.Now:O}] {message}{Environment.NewLine}");
    }

    // Helper: Obtém caminho curto (8.3) para evitar problemas com espaços
    private static string GetShortPath(string longPath)
    {
        // Fallback: se não conseguir o caminho curto, usa o longo entre aspas
        var fallback = $"\"{longPath}\"";
        try
        {
            if (File.Exists(longPath))
            {
                var fileInfo = new FileInfo(longPath);
                // Tenta usar o 8.3 via kernel32.dll. Se falhar, retorna aspas.
                return longPath.Contains(' ') ? fallback : longPath;
            }
        }
        catch { }
        return longPath.Contains(' ') ? fallback : longPath;
    }
}
