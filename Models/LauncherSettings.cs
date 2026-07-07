using System.Collections.Generic;

namespace NexusLauncher.Models;

public class LauncherSettings
{
    public string Nickname { get; set; } = "Player";
    public int AllocatedRamGb { get; set; } = 4;
    public bool CloseLauncherOnGameStart { get; set; }
    public bool ReopenLauncherAfterGameClose { get; set; } = true;
    public bool MinimizeLauncherOnGameStart { get; set; } = true;
    public bool Fullscreen { get; set; }
    public string Resolution { get; set; } = "1280x720";
    public string LastPlayedVersion { get; set; } = string.Empty;
    public string MinecraftDirectory { get; set; } = string.Empty;
    public string LastVersionCategory { get; set; } = "Releases";
    public string JavaPath { get; set; } = string.Empty;
    public string Theme { get; set; } = "Dark";
    public string Language { get; set; } = "pt-BR";
    public bool KeepLauncherInBackground { get; set; } = true;
    public List<string> FavoriteVersions { get; set; } = new();
    public List<string> EnabledCategories { get; set; } = new();
    public string CurseForgeApiKey { get; set; } = string.Empty;
    public string JvmArgs { get; set; } = string.Empty;
    public bool DownloadJavaAutomatically { get; set; } = true;
    public bool AutoUpdateLauncher { get; set; } = true;
    public List<string> FriendUsernames { get; set; } = new();
    public MinecraftAccount? Account { get; set; }
}
