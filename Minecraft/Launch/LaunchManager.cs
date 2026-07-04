using CmlLib.Core;
using CmlLib.Core.Auth;
using CmlLib.Core.ProcessBuilder;
using System;
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

    public async Task<bool> LaunchAsync(string version, string username, string javaPath)
    {
        try
        {
            var safeUserName = string.IsNullOrWhiteSpace(username) ? "Player" : username.Trim();
            var option = new MLaunchOption
            {
                Session = MSession.CreateOfflineSession(safeUserName),
                JavaPath = javaPath,
                MaximumRamMb = 2048,
                ScreenWidth = 1280,
                ScreenHeight = 720
            };

            var process = await _launcher.BuildProcessAsync(version, option);
            process.EnableRaisingEvents = true;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;
            process.OutputDataReceived += (_, args) => AppendLog(version, args.Data);
            process.ErrorDataReceived += (_, args) => AppendLog(version, args.Data);
            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            return true;
        }
        catch (Exception ex)
        {
            AppendLog(version, ex.ToString());
            return false;
        }
    }

    private static void AppendLog(string version, string? message)
    {
        if (string.IsNullOrWhiteSpace(message)) return;
        Directory.CreateDirectory(MinecraftPaths.LaunchLogs);
        var logFile = Path.Combine(MinecraftPaths.LaunchLogs, $"{version}.log");
        File.AppendAllText(logFile, $"[{DateTimeOffset.Now:O}] {message}{Environment.NewLine}");
    }
}
