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
    public List<string> FavoriteVersions { get; set; } = new();
}
