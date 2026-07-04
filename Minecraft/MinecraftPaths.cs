using CmlLib.Core;
using NexusLauncher.Services;
using System;
using System.IO;

namespace NexusLauncher.Minecraft;

public static class MinecraftPaths
{
    public static string Root => GetConfiguredGameRoot();
    public static string Assets => Path.Combine(Root, "assets");
    public static string Versions => Path.Combine(Root, "versions");
    public static string Libraries => Path.Combine(Root, "libraries");
    public static string ResourcePacks => Path.Combine(Root, "resourcepacks");
    public static string ShaderPacks => Path.Combine(Root, "shaderpacks");
    public static string Saves => Path.Combine(Root, "saves");
    public static string Screenshots => Path.Combine(Root, "screenshots");

    public static string InternalRoot => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Minecraft");
    public static string Cache => Path.Combine(InternalRoot, "Cache");
    public static string Temp => Path.Combine(InternalRoot, "Temp");
    public static string Downloads => Path.Combine(InternalRoot, "Downloads");
    public static string Java => Path.Combine(InternalRoot, "Java");
    public static string Icons => Path.Combine(InternalRoot, "Icons");
    public static string LaunchLogs => Path.Combine(InternalRoot, "Logs", "Launch");

    public static MinecraftPath GamePath => CreateGamePath();

    public static string GetOfficialDefaultRoot()
    {
        if (OperatingSystem.IsWindows())
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), ".minecraft");
        if (OperatingSystem.IsMacOS())
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Library", "Application Support", "minecraft");
        return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".minecraft");
    }

    public static string GetConfiguredGameRoot()
    {
        var configured = new SettingsService().Load().MinecraftDirectory;
        return string.IsNullOrWhiteSpace(configured) ? GetOfficialDefaultRoot() : configured;
    }

    public static void EnsureInternalDirs()
    {
        Directory.CreateDirectory(Cache);
        Directory.CreateDirectory(Temp);
        Directory.CreateDirectory(Downloads);
        Directory.CreateDirectory(Java);
        Directory.CreateDirectory(Icons);
        Directory.CreateDirectory(LaunchLogs);
    }

    private static MinecraftPath CreateGamePath()
    {
        EnsureInternalDirs();
        var path = new MinecraftPath(Root)
        {
            Assets = Assets,
            Versions = Versions
        };
        path.CreateDirs();
        Directory.CreateDirectory(ResourcePacks);
        Directory.CreateDirectory(ShaderPacks);
        Directory.CreateDirectory(Saves);
        Directory.CreateDirectory(Screenshots);
        return path;
    }
}
