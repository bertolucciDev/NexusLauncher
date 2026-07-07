using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NexusLauncher.Models;
using NexusLauncher.Services;
using NexusLauncher.ViewModels.Base;
using System;
using System.Threading.Tasks;

namespace NexusLauncher.ViewModels;

public partial class ProfileViewModel : ViewModelBase
{
    private readonly AccountService _account;
    private readonly SkinService _skin;

    [ObservableProperty] private string title = "Perfil";
    [ObservableProperty] private string statusMessage = string.Empty;
    [ObservableProperty] private bool isBusy;
    [ObservableProperty] private bool isLoggedIn;
    [ObservableProperty] private bool isMicrosoft;
    [ObservableProperty] private bool isOffline;
    [ObservableProperty] private string accountName = string.Empty;
    [ObservableProperty] private string accountTypeLabel = "Offline";
    [ObservableProperty] private string nickname = "Player";
    [ObservableProperty] private bool hasMicrosoftRefresh;
    [ObservableProperty] private Avalonia.Media.Imaging.Bitmap? avatarBitmap;

    public ProfileViewModel()
    {
        _account = LauncherRuntime.Account;
        _skin = new SkinService();
        _account.Load();
        _ = RefreshFromAccountAsync();
    }

    private async Task RefreshFromAccountAsync()
    {
        var acc = _account.Current;
        if (acc is not null && !string.IsNullOrEmpty(acc.Username))
        {
            IsLoggedIn = true;
            IsMicrosoft = acc.Type == "microsoft";
            IsOffline = acc.Type == "offline";
            AccountName = acc.Username;
            AccountTypeLabel = IsMicrosoft ? "Conta Microsoft" : "Offline";
            HasMicrosoftRefresh = IsMicrosoft && !string.IsNullOrEmpty(acc.MicrosoftRefreshToken);
            Nickname = acc.Username;
            SaveNickname();
            await LoadAvatarAsync(acc.Username);
        }
        else
        {
            IsLoggedIn = false;
            IsMicrosoft = false;
            IsOffline = false;
            AccountName = string.Empty;
            AccountTypeLabel = "Nao conectado";
            HasMicrosoftRefresh = false;
            AvatarBitmap = null;
        }
    }

    private async Task LoadAvatarAsync(string username)
    {
        AvatarBitmap = await _skin.GetAvatarAsync(username);
    }

    [RelayCommand]
    private async Task LoginMicrosoftAsync()
    {
        IsBusy = true;
        StatusMessage = "Solicitando código de dispositivo...";

        var progress = new Progress<string>(msg =>
        {
            Avalonia.Threading.Dispatcher.UIThread.Post(() => StatusMessage = msg);
        });

        try
        {
            await _account.LoginMicrosoftAsync(progress);
            await RefreshFromAccountAsync();
            StatusMessage = "Conectado com sucesso!";
        }
        catch (OperationCanceledException)
        {
            StatusMessage = "Login cancelado.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Erro: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private void LoginOffline()
    {
        var name = string.IsNullOrWhiteSpace(Nickname) ? "Player" : Nickname.Trim();
        _account.SetOffline(name);
        _ = RefreshFromAccountAsync();
        StatusMessage = $"Modo offline como {name}";
    }

    [RelayCommand]
    private void Logout()
    {
        _account.Logout();
        _ = RefreshFromAccountAsync();
        Nickname = "Player";
        SaveNickname();
        StatusMessage = "Desconectado.";
    }

    [RelayCommand]
    private async Task RefreshMicrosoftAsync()
    {
        IsBusy = true;
        StatusMessage = "Atualizando sessao...";
        try
        {
            var ok = await _account.TryRefreshMicrosoftAsync();
            if (ok)
            {
                await RefreshFromAccountAsync();
                StatusMessage = "Sessao atualizada!";
            }
            else
            {
                StatusMessage = "Falha ao renovar. Faca login novamente.";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Erro: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    partial void OnNicknameChanged(string value)
    {
        SaveNickname();
    }

    private void SaveNickname()
    {
        var s = LauncherRuntime.Settings.Load();
        s.Nickname = string.IsNullOrWhiteSpace(Nickname) ? "Player" : Nickname.Trim();
        LauncherRuntime.Settings.Save(s);
    }
}
