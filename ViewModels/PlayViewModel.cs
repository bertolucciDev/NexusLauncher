using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NexusLauncher.Models;
using NexusLauncher.Minecraft;
using NexusLauncher.Storage;
using NexusLauncher.ViewModels.Base;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace NexusLauncher.ViewModels;

public partial class PlayViewModel : ViewModelBase
{
    private readonly MinecraftService _minecraft = new();
    private readonly SettingsStorage _settingsStorage = new();

    [ObservableProperty]
    private string status = "Pronto";

    [ObservableProperty]
    private string nickname = "Player";

    [ObservableProperty]
    private string? selectedVersion;

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

    partial void OnSelectedVersionChanged(string? value)
    {
        UpdateButtonText();
    }

    private Task LoadVersionsAsync()
    {
        StateStyle = "Loading";
        Status = "Buscando versões...";

        var installed = _minecraft.GetInstalledVersions();
        var versions = new[] { "1.20.1", "1.20.4", "1.20.6", "1.21" };

        Versions.Clear();
        foreach (var version in versions)
        {
            Versions.Add(new MinecraftVersionInfo
            {
                Id = version,
                Type = "release",
                IsInstalled = installed.Contains(version)
            });
        }

        if (Versions.Count > 0)
            SelectedVersion = Versions[0].Id;

        StateStyle = "Ready";
        Status = "Versões carregadas";
        UpdateButtonText();
        return Task.CompletedTask;
    }

    private void UpdateButtonText()
    {
        var version = SelectedVersion;
        ButtonText = string.IsNullOrWhiteSpace(version)
            ? "Selecionar"
            : (_minecraft.IsVersionInstalled(version) ? "Jogar" : "Instalar");
    }

    [RelayCommand]
    public async Task PrimaryAction()
    {
        if (string.IsNullOrWhiteSpace(SelectedVersion))
        {
            Status = "Selecione uma versão";
            StateStyle = "Error";
            UpdateButtonText();
            return;
        }

        _settingsStorage.Save(Nickname);
        ButtonText = _minecraft.IsVersionInstalled(SelectedVersion) ? "Jogar" : "Instalar";
        Status = "Preparando Minecraft...";
        StateStyle = "Loading";

        try
        {
            var ready = await _minecraft.EnsureVersionReadyAsync(SelectedVersion, Nickname);
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