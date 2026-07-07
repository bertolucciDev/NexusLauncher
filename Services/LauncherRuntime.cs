using System.Net.Http;

namespace NexusLauncher.Services;

public static class LauncherRuntime
{
    private static readonly HttpClient HttpClient = new();

    public static ProcessService Processes { get; } = new();
    public static SettingsService Settings { get; } = new();
    public static AccountService Account { get; } = new(Settings, new MinecraftAuthService(HttpClient));
    public static NexuSkinService NexuSkin { get; } = new();
}
