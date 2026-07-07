namespace NexusLauncher.Models;

public class MinecraftAccount
{
    public string Type { get; set; } = "offline";
    public string Username { get; set; } = string.Empty;
    public string Uuid { get; set; } = string.Empty;
    public string MicrosoftRefreshToken { get; set; } = string.Empty;
    public string MinecraftAccessToken { get; set; } = string.Empty;
}
