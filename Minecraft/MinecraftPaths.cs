using CmlLib.Core;
using System;
using System.IO;

namespace NexusLauncher.Minecraft;

public static class MinecraftPaths
{
    public static string Root { get; } = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Minecraft");
    public static string LaunchLogs { get; } = Path.Combine(Root, "Launch");
    public static MinecraftPath GamePath { get; } = CreateGamePath();

    private static MinecraftPath CreateGamePath()
    {
        Directory.CreateDirectory(Root);
        Directory.CreateDirectory(LaunchLogs);
        Directory.CreateDirectory(Path.Combine(Root, "Mods"));
        Directory.CreateDirectory(Path.Combine(Root, "Java"));
        var path = new MinecraftPath(Root);
        path.CreateDirs();
        return path;
    }
}
