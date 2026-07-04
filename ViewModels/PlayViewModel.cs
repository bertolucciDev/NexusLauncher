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

    public ObservableCollection<MinecraftVersionInfo> Versions { get; } = new();

    public string SelectedVersionTitle => SelectedVersion is null ? "Selecione uma versão" : SelectedVersion.Id;

    public string SelectedVersionKind => SelectedVersion is null ? "Release" : $"{SelectedVersion.BadgeIcon} {SelectedVersion.BaseVersionText} • {ToFriendlyType(SelectedVersion)}";

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
        var settings = _settingsStorage.Load();
        settings.Nickname = string.IsNullOrWhiteSpace(value) ? "Player" : value.Trim();
        _settingsStorage.Save(settings);
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

        var settings = _settingsStorage.Load();
        SelectedVersion = Versions.FirstOrDefault(v => v.Id == settings.LastPlayedVersion) ?? Versions.FirstOrDefault(v => v.IsInstalled) ?? Versions.FirstOrDefault();
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
        if (!string.IsNullOrWhiteSpace(version) && !_minecraft.IsJavaReadyFor(version))
        {
            ButtonText = "INSTALAR JAVA";
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
        HeaderSubtitle = SelectedVersion is null ? Nickname : $"{Nickname} • {SelectedVersion.Id}";
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
        var wasInstalled = _minecraft.IsVersionInstalled(selectedVersion.Id);
        var settings = _settingsStorage.Load();
        settings.Nickname = Nickname;
        _settingsStorage.Save(settings);

        try
        {
            ButtonText = _minecraft.IsVersionInstalled(selectedVersion.Id) ? "VERIFICANDO..." : "BAIXANDO...";
            SetProgress("Preparando ambiente", 18, "Verificando Java", "", "");

            if (!_minecraft.IsJavaReadyFor(selectedVersion.Id))
            {
                SetProgress("Java necessário", 62, "Instalação automática de Java será baixada na próxima etapa", "", "");
                await Task.Delay(700);
                IsBusy = false;
                ApplyReadyHeader();
                ButtonText = "INSTALAR JAVA";
                return;
            }

            if (!_minecraft.IsVersionInstalled(selectedVersion.Id))
                SetProgress($"Instalando Minecraft {selectedVersion.Id}", 53, "Arquivos: preparando download", "Velocidade: calculando", "Tempo restante: calculando");
            else
                SetProgress("Preparando ambiente", 84, "Carregando perfil", "", "");

            var progress = new Progress<DownloadProgressInfo>(ApplyDownloadProgress);
            var process = await _minecraft.PrepareAndLaunchAsync(selectedVersion.Id, Nickname, settings, progress);
            selectedVersion.IsInstalled = process is not null || _minecraft.IsVersionInstalled(selectedVersion.Id);
            if (selectedVersion.IsInstalled && !wasInstalled)
                await LoadVersionsAsync();
            if (process is not null)
            {
                settings.LastPlayedVersion = selectedVersion.Id;
                _settingsStorage.Save(settings);
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

    private void ApplyDownloadProgress(DownloadProgressInfo progress)
    {
        IsBusy = true;
        HeaderTitle = progress.State == "Concluído" ? "Instalação concluída" : "Baixando...";
        ProgressValue = Math.Clamp(progress.Percent, 0, 100);
        ProgressPercent = $"{ProgressValue:0}%";
        ProgressDetail = string.IsNullOrWhiteSpace(progress.CurrentFile) ? "Preparando arquivos" : progress.CurrentFile;
        ProgressSpeed = progress.BytesPerSecond > 0 ? $"{FormatBytes(progress.BytesPerSecond)}/s" : "Calculando velocidade";
        ProgressEta = progress.Eta.HasValue ? $"Restante: {FormatEta(progress.Eta.Value)}" : "Restante: calculando";
        ButtonText = progress.State == "Concluído" ? "JOGAR" : "BAIXANDO...";
    }

    private static string FormatBytes(double bytes)
    {
        string[] units = { "B", "KB", "MB", "GB" };
        var value = bytes;
        var unit = 0;
        while (value >= 1024 && unit < units.Length - 1) { value /= 1024; unit++; }
        return $"{value:0.##} {units[unit]}";
    }

    private static string FormatEta(TimeSpan eta) => eta <= TimeSpan.Zero ? "0s" : eta.TotalMinutes >= 1 ? $"{eta.TotalMinutes:0} min" : $"{eta.TotalSeconds:0}s";

    private static string ToFriendlyType(MinecraftVersionInfo version)
    {
        return version.Category switch
        {
            MinecraftVersionCategory.Forge => "Forge",
            MinecraftVersionCategory.NeoForge => "NeoForge",
            MinecraftVersionCategory.Fabric => "Fabric",
            MinecraftVersionCategory.Quilt => "Quilt",
            MinecraftVersionCategory.OptiFine => "OptiFine",
            MinecraftVersionCategory.LiteLoader => "LiteLoader",
            MinecraftVersionCategory.Modpack => "Modpack",
            MinecraftVersionCategory.Custom => "Custom",
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
