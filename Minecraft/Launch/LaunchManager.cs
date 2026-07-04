using CmlLib.Core;
using CmlLib.Core.Auth;
using CmlLib.Core.ProcessBuilder;
using NexusLauncher.Models;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace NexusLauncher.Minecraft.Launch;

public class LaunchManager
{
    private readonly MinecraftLauncher _launcher;

    public LaunchManager(MinecraftPath? path = null)
    {
        _launcher = new MinecraftLauncher(path ?? MinecraftPaths.GamePath);
    }

    public async Task<Process?> LaunchAsync(string version, string username, string javaPath, LauncherSettings settings)
    {
        try
        {
            var safeUserName = string.IsNullOrWhiteSpace(username) ? "Player" : username.Trim();
            var (width, height) = ParseResolution(settings.Resolution);
            var option = new MLaunchOption
            {
                Session = MSession.CreateOfflineSession(safeUserName),
                JavaPath = javaPath,
                MaximumRamMb = settings.AllocatedRamGb * 1024,
                ScreenWidth = width,
                ScreenHeight = height,
                FullScreen = settings.Fullscreen
            };

            var process = await _launcher.BuildProcessAsync(version, option);
            process.EnableRaisingEvents = true;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.CreateNoWindow = true;
            process.StartInfo.WorkingDirectory = MinecraftPaths.Root;
            process.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;
            process.OutputDataReceived += (_, args) => AppendLog(version, args.Data);
            process.ErrorDataReceived += (_, args) => AppendLog(version, args.Data);

            if (!process.Start())
                return null;

            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            AppendLog(version, $"Minecraft process started. PID={process.Id}");
            return process;
        }
        catch (Exception ex)
        {
            AppendLog(version, ex.ToString());
            return null;
        }
    }

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
        var logFile = Path.Combine(MinecraftPaths.LaunchLogs, $"{version}.log");
        File.AppendAllText(logFile, $"[{DateTimeOffset.Now:O}] {message}{Environment.NewLine}");
    }
}
