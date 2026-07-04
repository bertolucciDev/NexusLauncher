using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NexusLauncher.Models;
using NexusLauncher.Services;
using NexusLauncher.Storage;
using NexusLauncher.ViewModels.Base;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace NexusLauncher.ViewModels;

public partial class PlayViewModel : ViewModelBase
{
    private readonly NexusLauncher.Minecraft.MinecraftService _minecraft = new();
    private readonly VersionService _versionService = new();
    private readonly SkinService _skinService = new();
    private readonly SettingsStorage _settingsStorage = new();

    [ObservableProperty]
    private string status = "Pronto";

    [ObservableProperty]
    private string nickname = "Player";

    [ObservableProperty]
    private Bitmap? avatarImage;

    [ObservableProperty]
    private MinecraftVersionInfo? selectedVersion;

    [ObservableProperty]
    private string buttonText = "Instalar";

    [ObservableProperty]
    private string stateStyle = "Idle";

    [ObservableProperty]
    private int? minecraftProcessId;

    public ObservableCollection<MinecraftVersionInfo> Versions { get; } = new();

    public PlayViewModel()
    {
        Nickname = _settingsStorage.LoadNickname();
        LauncherRuntime.Processes.MinecraftExited += (_, _) =>
        {
            MinecraftProcessId = null;
            Status = "Minecraft fechado";
            StateStyle = "Ready";
        };
        _ = LoadVersionsAsync();
        _ = RefreshSkinAsync();
        UpdateButtonText();
    }

    partial void OnSelectedVersionChanged(MinecraftVersionInfo? value)
    {
        UpdateButtonText();
    }

    partial void OnNicknameChanged(string value)
    {
        _ = RefreshSkinAsync();
    }

    private async Task LoadVersionsAsync()
    {
        StateStyle = "Loading";
        Status = "Buscando versões oficiais e locais...";

        Versions.Clear();
        foreach (var version in _versionService.GetInstalledVersionInfos())
            Versions.Add(version);

        var installedIds = Versions.Select(v => v.Id).ToHashSet();
        var officialVersions = await _versionService.GetOfficialVersionsAsync();
        foreach (var version in officialVersions.Where(v => !installedIds.Contains(v.Id)).Take(80))
            Versions.Add(version);

        SelectedVersion = Versions.FirstOrDefault(v => v.IsInstalled) ?? Versions.FirstOrDefault();
        StateStyle = Versions.Count > 0 ? "Ready" : "Error";
        Status = Versions.Count > 0 ? "Versões carregadas" : "Não foi possível carregar versões";
        UpdateButtonText();
    }

    private async Task RefreshSkinAsync()
    {
        AvatarImage = await _skinService.GetAvatarAsync(Nickname);
    }

    private void UpdateButtonText()
    {
        var version = SelectedVersion?.Id;
        if (!_minecraft.IsJavaReady())
        {
            ButtonText = "Instalar Java";
            return;
        }

        ButtonText = string.IsNullOrWhiteSpace(version)
            ? "Selecionar versão"
            : (_minecraft.IsVersionInstalled(version) ? "Jogar" : "Instalar Minecraft");
    }

    [RelayCommand]
    public async Task PrimaryAction()
    {
        if (string.IsNullOrWhiteSpace(SelectedVersion?.Id))
        {
            Status = "Selecione uma versão";
            StateStyle = "Error";
            UpdateButtonText();
            return;
        }

        var selectedVersion = SelectedVersion!;
        var settings = _settingsStorage.Load();
        settings.Nickname = Nickname;
        _settingsStorage.Save(settings);

        ButtonText = _minecraft.IsVersionInstalled(selectedVersion.Id) ? "Jogar" : "Instalar Minecraft";
        Status = "Preparando Minecraft...";
        StateStyle = "Loading";

        try
        {
            var process = await _minecraft.PrepareAndLaunchAsync(selectedVersion.Id, Nickname, settings);
            selectedVersion.IsInstalled = process is not null || _minecraft.IsVersionInstalled(selectedVersion.Id);
            MinecraftProcessId = process?.Id;
            Status = process is not null ? $"Minecraft iniciado (PID {process.Id})" : _minecraft.GetStatusMessage();
            StateStyle = process is not null ? "Playing" : "Error";
            UpdateButtonText();
        }
        catch (System.Exception ex)
        {
            Status = ex.Message;
            StateStyle = "Error";
        }
    }
}
