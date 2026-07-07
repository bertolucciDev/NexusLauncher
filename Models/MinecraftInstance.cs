using System;

namespace NexusLauncher.Models;

public class MinecraftInstance
{
    public string Name { get; set; } = "";
    public string MinecraftVersion { get; set; } = "";
    public string Loader { get; set; } = "";
    public string LoaderVersion { get; set; } = "";
    public string Icon { get; set; } = "";
    public string Path { get; set; } = "";
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public string CreatedAtText => CreatedAt.ToString("dd/MM/yyyy");
    public string VersionDisplay => string.IsNullOrWhiteSpace(MinecraftVersion) ? "—" : MinecraftVersion;
    public string LoaderDisplay => string.IsNullOrWhiteSpace(Loader) ? "Vanilla" : Loader;
}