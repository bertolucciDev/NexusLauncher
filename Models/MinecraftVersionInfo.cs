namespace NexusLauncher.Models;

public class MinecraftVersionInfo
{
    public string Id { get; set; } = string.Empty;
    public string Type { get; set; } = "release";
    public bool IsInstalled { get; set; }
    public MinecraftVersionCategory Category { get; set; } = MinecraftVersionCategory.Release;
    public string DisplayName => IsInstalled ? $"{Id} • instalado" : Id;
}
