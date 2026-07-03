namespace NexusLauncher.Models;

public class MinecraftVersionInfo
{
    public string Id { get; set; } = string.Empty;
    public string Type { get; set; } = "release";
    public bool IsInstalled { get; set; }
}