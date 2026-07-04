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
    public string LastVersionCategory { get; set; } = "Releases";
    public string JavaPath { get; set; } = string.Empty;
    public string Theme { get; set; } = "Dark";
    public bool KeepLauncherInBackground { get; set; } = true;
    public List<string> FavoriteVersions { get; set; } = new();
}
