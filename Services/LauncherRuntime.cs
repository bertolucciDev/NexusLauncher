namespace NexusLauncher.Services;

public static class LauncherRuntime
{
    public static ProcessService Processes { get; } = new();
    public static SettingsService Settings { get; } = new();
}
