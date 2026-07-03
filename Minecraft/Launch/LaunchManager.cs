using CmlLib.Core;
using CmlLib.Core.Auth;
using CmlLib.Core.ProcessBuilder;
using System;
using System.Threading.Tasks;

namespace NexusLauncher.Minecraft.Launch;

public class LaunchManager
{
    private readonly MinecraftLauncher _launcher;

    public LaunchManager(string? minecraftRoot = null)
    {
        _launcher = new MinecraftLauncher(new MinecraftPath());
    }

    public async Task<bool> LaunchAsync(string version, string username, string javaPath)
    {
        try
        {
            var option = new MLaunchOption
            {
                Session = MSession.CreateOfflineSession(username),
                JavaPath = javaPath,
                MaximumRamMb = 2048,
                ScreenWidth = 1280,
                ScreenHeight = 720
            };

            var process = await _launcher.CreateProcessAsync(version, option);
            process.Start();
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
            return false;
        }
    }
}
