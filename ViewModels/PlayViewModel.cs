using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NexusLauncher.Models;
using NexusLauncher.Minecraft;
using NexusLauncher.Storage;
using NexusLauncher.ViewModels.Base;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace NexusLauncher.ViewModels;

public partial class PlayViewModel : ViewModelBase
{
    private readonly MinecraftService _minecraft = new();
    private readonly NexusLauncher.Services.VersionService _versionService = new();
    private readonly SettingsStorage _settingsStorage = new();

    [ObservableProperty]
    private string status = "Pronto";

    [ObservableProperty]
    private string nickname = "Player";

    [ObservableProperty]
    private MinecraftVersionInfo? selectedVersion;

    [ObservableProperty]
    private string buttonText = "Instalar";

    [ObservableProperty]
    private string stateStyle = "Idle";

    public ObservableCollection<MinecraftVersionInfo> Versions { get; } = new();

    public PlayViewModel()
    {
        Nickname = _settingsStorage.LoadNickname();
        _ = LoadVersionsAsync();
        UpdateButtonText();
    }

    partial void OnSelectedVersionChanged(MinecraftVersionInfo? value)
    {
        UpdateButtonText();
    }

    private async Task LoadVersionsAsync()
    {
        StateStyle = "Loading";
        Status = "Buscando versões oficiais...";

        Versions.Clear();
        var versions = await _versionService.GetOfficialVersionsAsync();
        foreach (var version in versions)
        {
            version.IsInstalled = _minecraft.IsVersionInstalled(version.Id);
            Versions.Add(version);
        }

        SelectedVersion = Versions.FirstOrDefault(v => v.IsInstalled) ?? Versions.FirstOrDefault();
        StateStyle = Versions.Count > 0 ? "Ready" : "Error";
        Status = Versions.Count > 0 ? "Versões carregadas" : "Não foi possível carregar versões";
        UpdateButtonText();
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
        _settingsStorage.Save(Nickname);
        ButtonText = _minecraft.IsVersionInstalled(selectedVersion.Id) ? "Jogar" : "Instalar Minecraft";
        Status = "Preparando Minecraft...";
        StateStyle = "Loading";

        try
        {
            var ready = await _minecraft.EnsureVersionReadyAsync(selectedVersion.Id, Nickname);
            selectedVersion.IsInstalled = ready || _minecraft.IsVersionInstalled(selectedVersion.Id);
            Status = ready ? "Minecraft pronto para iniciar" : _minecraft.GetStatusMessage();
            StateStyle = ready ? "Ready" : "Error";
            UpdateButtonText();
        }
        catch (System.Exception ex)
        {
            Status = ex.Message;
            StateStyle = "Error";
        }
    }
}