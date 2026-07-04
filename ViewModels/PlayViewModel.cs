using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NexusLauncher.Models;
using NexusLauncher.Services;
using NexusLauncher.Storage;
using NexusLauncher.ViewModels.Base;
using System;
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
    private string nickname = "Player";

    [ObservableProperty]
    private Bitmap? avatarImage;

    [ObservableProperty]
    private MinecraftVersionInfo? selectedVersion;

    [ObservableProperty]
    private string buttonText = "JOGAR";

    [ObservableProperty]
    private string headerTitle = "Pronto para jogar";

    [ObservableProperty]
    private string headerSubtitle = string.Empty;

    [ObservableProperty]
    private bool isBusy;

    [ObservableProperty]
    private double progressValue;

    [ObservableProperty]
    private string progressPercent = "0%";

    [ObservableProperty]
    private string progressDetail = string.Empty;

    [ObservableProperty]
    private string progressSpeed = string.Empty;

    [ObservableProperty]
    private string progressEta = string.Empty;

    [ObservableProperty]
    private string lastUpdateText = "Última atualização — hoje";

    [ObservableProperty]
    private int? minecraftProcessId;

    public ObservableCollection<MinecraftVersionInfo> Versions { get; } = new();

    public string SelectedVersionTitle => SelectedVersion is null ? "Selecione uma versão" : $"Minecraft {SelectedVersion.Id}";

    public string SelectedVersionKind => SelectedVersion is null ? "Release" : $"{ToFriendlyType(SelectedVersion)} {SelectedVersion.Id}";

    public PlayViewModel()
    {
        Nickname = _settingsStorage.LoadNickname();
        LauncherRuntime.Processes.MinecraftExited += (_, _) =>
        {
            IsBusy = false;
            ProgressValue = 0;
            ProgressPercent = "0%";
            ApplyReadyHeader();
            UpdateButtonText();
        };
        _ = LoadVersionsAsync();
        _ = RefreshSkinAsync();
        UpdateButtonText();
    }

    partial void OnSelectedVersionChanged(MinecraftVersionInfo? value)
    {
        OnPropertyChanged(nameof(SelectedVersionTitle));
        OnPropertyChanged(nameof(SelectedVersionKind));
        ApplyReadyHeader();
        UpdateButtonText();
    }

    partial void OnNicknameChanged(string value)
    {
        ApplyReadyHeader();
        _ = RefreshSkinAsync();
    }

    private async Task LoadVersionsAsync()
    {
        SetProgress("Verificando biblioteca", 12, "Preparando versões", "", "");
        ButtonText = "VERIFICANDO...";

        Versions.Clear();
        foreach (var version in _versionService.GetInstalledVersionInfos())
            Versions.Add(version);

        var installedIds = Versions.Select(v => v.Id).ToHashSet();
        var officialVersions = await _versionService.GetOfficialVersionsAsync();
        foreach (var version in officialVersions.Where(v => !installedIds.Contains(v.Id)).Take(80))
            Versions.Add(version);

        SelectedVersion = Versions.FirstOrDefault(v => v.IsInstalled) ?? Versions.FirstOrDefault();
        IsBusy = false;
        ProgressValue = 0;
        ProgressPercent = "0%";
        LastUpdateText = $"Última atualização — {DateTime.Now:dd/MM/yyyy}";
        ApplyReadyHeader();
        UpdateButtonText();
    }

    private async Task RefreshSkinAsync()
    {
        AvatarImage = await _skinService.GetAvatarAsync(Nickname);
    }

    private void UpdateButtonText()
    {
        if (IsBusy)
            return;

        var version = SelectedVersion?.Id;
        if (!_minecraft.IsJavaReady())
        {
            ButtonText = "INSTALAR";
            return;
        }

        ButtonText = string.IsNullOrWhiteSpace(version)
            ? "VERIFICAR"
            : (_minecraft.IsVersionInstalled(version) ? "JOGAR" : "INSTALAR");
    }

    private void ApplyReadyHeader()
    {
        if (IsBusy)
            return;

        HeaderTitle = GetGreeting();
        HeaderSubtitle = SelectedVersion is null ? Nickname : $"{Nickname} • Minecraft {SelectedVersion.Id}";
        ProgressDetail = string.Empty;
        ProgressSpeed = string.Empty;
        ProgressEta = string.Empty;
    }

    private void SetProgress(string title, double value, string detail, string speed, string eta)
    {
        IsBusy = true;
        HeaderTitle = title;
        HeaderSubtitle = SelectedVersion is null ? "Preparando Minecraft" : $"Minecraft {SelectedVersion.Id}";
        ProgressValue = Math.Clamp(value, 0, 100);
        ProgressPercent = $"{(int)ProgressValue}%";
        ProgressDetail = detail;
        ProgressSpeed = speed;
        ProgressEta = eta;
    }

    [RelayCommand]
    public async Task PrimaryAction()
    {
        if (SelectedVersion is null)
        {
            SetProgress("Verificando versões", 20, "Escolha uma versão para continuar", "", "");
            await Task.Delay(600);
            IsBusy = false;
            ApplyReadyHeader();
            UpdateButtonText();
            return;
        }

        var selectedVersion = SelectedVersion;
        var settings = _settingsStorage.Load();
        settings.Nickname = Nickname;
        _settingsStorage.Save(settings);

        try
        {
            ButtonText = _minecraft.IsVersionInstalled(selectedVersion.Id) ? "VERIFICANDO..." : "BAIXANDO...";
            SetProgress("Preparando ambiente", 18, "Verificando Java", "", "");

            if (!_minecraft.IsJavaReady())
            {
                SetProgress("Instalando Java", 62, "Baixando Java", "", "");
                await Task.Delay(700);
                IsBusy = false;
                ApplyReadyHeader();
                ButtonText = "INSTALAR";
                return;
            }

            if (!_minecraft.IsVersionInstalled(selectedVersion.Id))
                SetProgress($"Instalando Minecraft {selectedVersion.Id}", 53, "Arquivos: preparando download", "Velocidade: calculando", "Tempo restante: calculando");
            else
                SetProgress("Preparando ambiente", 84, "Carregando perfil", "", "");

            var process = await _minecraft.PrepareAndLaunchAsync(selectedVersion.Id, Nickname, settings);
            selectedVersion.IsInstalled = process is not null || _minecraft.IsVersionInstalled(selectedVersion.Id);
            if (process is not null)
            {
                SetProgress("Concluído", 100, "Abrindo Minecraft", "", "");
                await Task.Delay(350);
                IsBusy = false;
                ApplyReadyHeader();
                ButtonText = "JOGAR";
                return;
            }

            IsBusy = false;
            HeaderTitle = "Não foi possível iniciar";
            HeaderSubtitle = "Confira os logs para detalhes";
            UpdateButtonText();
        }
        catch (Exception)
        {
            IsBusy = false;
            HeaderTitle = "Não foi possível iniciar";
            HeaderSubtitle = "Confira os logs para detalhes";
            UpdateButtonText();
        }
    }

    private static string ToFriendlyType(MinecraftVersionInfo version)
    {
        return version.Category switch
        {
            MinecraftVersionCategory.Modded => "Modded",
            MinecraftVersionCategory.OptiFine => "OptiFine",
            _ => version.Type == "release" ? "Release" : version.Type
        };
    }

    private static string GetGreeting()
    {
        var hour = DateTime.Now.Hour;
        if (hour < 12) return "Bom dia!";
        if (hour < 18) return "Boa tarde!";
        return "Boa noite!";
    }
}
