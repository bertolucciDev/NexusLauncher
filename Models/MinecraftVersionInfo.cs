namespace NexusLauncher.Models;

public class MinecraftVersionInfo
{
    public string Id { get; set; } = string.Empty;
    public string Type { get; set; } = "release";
    public bool IsInstalled { get; set; }
    public MinecraftVersionCategory Category { get; set; } = MinecraftVersionCategory.Release;
    public string BaseVersion { get; set; } = string.Empty;
    public string Loader { get; set; } = "Vanilla";
    public string LoaderVersion { get; set; } = string.Empty;
    public string BadgeIcon { get; set; } = "🟩";
    public bool IsFavorite { get; set; }
    public bool IsLastPlayed { get; set; }
    public string DisplayName => Id;
    public string FriendlyType => Category switch
    {
        MinecraftVersionCategory.Release => "Release",
        MinecraftVersionCategory.Snapshot => "Snapshot",
        MinecraftVersionCategory.Modpack => "Modpack",
        MinecraftVersionCategory.Custom => "Custom",
        _ => Loader
    };
    public string BaseVersionText => string.IsNullOrWhiteSpace(BaseVersion) ? FriendlyType : $"Minecraft {BaseVersion}";
    public string FavoriteIcon => IsFavorite ? "⭐" : "☆";
}
