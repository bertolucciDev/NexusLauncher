using NexusLauncher.Models;
using System;
using System.Threading.Tasks;

namespace NexusLauncher.Services;

public class AccountService
{
    private readonly SettingsService _settings;
    private readonly MinecraftAuthService _auth;
    private MinecraftAccount? _current;

    public MinecraftAccount? Current => _current;

    public AccountService(SettingsService settings, MinecraftAuthService auth)
    {
        _settings = settings;
        _auth = auth;
    }

    public void Load()
    {
        var s = _settings.Load();
        _current = s.Account;
        if (_current is not null && string.IsNullOrEmpty(_current.Username))
            _current = null;
    }

    public void SetOffline(string username)
    {
        _current = new MinecraftAccount
        {
            Type = "offline",
            Username = username,
            Uuid = string.Empty
        };
        Save();
    }

    public async Task<MinecraftAccount> LoginMicrosoftAsync(IProgress<string>? progress = null)
    {
        var account = await _auth.LoginMicrosoftAsync(progress);
        _current = account;
        Save();
        return account;
    }

    public async Task<bool> TryRefreshMicrosoftAsync()
    {
        if (_current?.Type != "microsoft")
            return false;

        try
        {
            var account = await _auth.RefreshAsync();
            _current = account;
            Save();
            return true;
        }
        catch
        {
            return false;
        }
    }

    public void Logout()
    {
        _current = null;
        Save();
    }

    private void Save()
    {
        var s = _settings.Load();
        s.Account = _current;
        _settings.Save(s);
    }
}
