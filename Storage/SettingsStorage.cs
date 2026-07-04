using NexusLauncher.Models;
using NexusLauncher.Services;

namespace NexusLauncher.Storage;

public class SettingsStorage
{
    private readonly SettingsService _settingsService;

    public SettingsStorage(string? path = null)
    {
        _settingsService = new SettingsService(path);
    }

    public void Save(string nickname)
    {
        var settings = _settingsService.Load();
        settings.Nickname = nickname;
        _settingsService.Save(settings);
    }

    public string LoadNickname() => _settingsService.Load().Nickname;

    public LauncherSettings Load() => _settingsService.Load();

    public void Save(LauncherSettings settings) => _settingsService.Save(settings);
}
