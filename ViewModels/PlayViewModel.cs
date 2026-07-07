using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NexusLauncher.Models;
using NexusLauncher.Services;
using NexusLauncher.Storage;
using NexusLauncher.ViewModels.Base;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using NexusLauncher.Minecraft;

namespace NexusLauncher.ViewModels;

public partial class PlayViewModel : ViewModelBase
{
    private readonly MinecraftService _minecraft = new();
    private readonly VersionService _versionService = new();
    private readonly SkinService _skinService = new();
    private readonly SettingsStorage _settingsStorage = new();
    private readonly InstanceService _instanceService = new(new SettingsService());

    [ObservableProperty] private string nickname = "Player";
    [ObservableProperty] private Avalonia.Media.Imaging.Bitmap? avatarImage;
    [ObservableProperty] private MinecraftVersionInfo? selectedVersion;
    [ObservableProperty] private string buttonText = LanguageService.Instance.T("play.button.play");
    [ObservableProperty] private string headerTitle = LanguageService.Instance.T("play.title");
    [ObservableProperty] private string headerSubtitle = string.Empty;
    [ObservableProperty] private bool isBusy;
    [ObservableProperty] private double progressValue;
    [ObservableProperty] private string progressPercent = "0%";
    [ObservableProperty] private string progressDetail = string.Empty;
    [ObservableProperty] private string progressSpeed = string.Empty;
    [ObservableProperty] private string progressEta = string.Empty;
    [ObservableProperty] private string lastUpdateText = "";

    public string PlayerAccountLabel => LanguageService.Instance.T("play.player_account");
    public string VersionLabel => LanguageService.Instance.T("play.version_label");
    public string DownloadsLabel => LanguageService.Instance.T("play.downloads");

    public PlayViewModel(bool autoStart = false)
    {
        Nickname = _settingsStorage.LoadNickname();
        LanguageService.Instance.PropertyChanged += (_, _) =>
        {
            HeaderTitle = LanguageService.Instance.T("play.title");
            ButtonText = LanguageService.Instance.T("play.button.play");
            OnPropertyChanged(nameof(PlayerAccountLabel));
            OnPropertyChanged(nameof(VersionLabel));
            OnPropertyChanged(nameof(DownloadsLabel));
        };

        LauncherRuntime.Processes.MinecraftExited += OnMinecraftExited;
        _ = LoadVersionsAsync().ContinueWith(async t =>
        {
            try
            {
                if (autoStart)
                    await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(PrimaryAction);
            }
            catch { }
        });
        _ = RefreshSkinAsync();
        UpdateButtonText();
    }

    private void OnMinecraftExited(object? sender, EventArgs e)
    {
        Avalonia.Threading.Dispatcher.UIThread.Post(NullProgress);
    }

    private void NullProgress()
    {
        IsBusy = false;
        ProgressValue = 0;
        ProgressPercent = "0%";
        ApplyReadyHeader();
        UpdateButtonText();
    }

    partial void OnSelectedVersionChanged(MinecraftVersionInfo? value)
    {
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
        var settings = _settingsStorage.Load();
        var enabled = GetEnabledCategories(settings);

        var installed = new List<MinecraftVersionInfo>();
        foreach (var v in _versionService.GetInstalledVersionInfos().Where(v => enabled.Contains(v.Category)))
            installed.Add(v);

        var official = await _versionService.GetOfficialVersionsAsync();
        var installedIds = installed.Select(v => v.Id).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var uninstalled = official.Where(v => !installedIds.Contains(v.Id) && enabled.Contains(v.Category)).Take(80).ToList();

        var custom = new List<MinecraftVersionInfo>();
        foreach (var inst in _instanceService.GetInstances())
        {
            if (installed.Any(v => v.InstancePath == inst.Path) || custom.Any(v => v.InstancePath == inst.Path)) continue;
            custom.Add(new MinecraftVersionInfo
            {
                Id = inst.Name,
                BaseVersion = inst.MinecraftVersion,
                Loader = string.IsNullOrWhiteSpace(inst.Loader) || inst.Loader == "vanilla" ? "Vanilla" : inst.Loader.First().ToString().ToUpper() + inst.Loader[1..],
                LoaderVersion = inst.LoaderVersion,
                Type = "custom",
                IsInstalled = true,
                IsLastPlayed = string.Equals(settings.LastPlayedVersion, inst.MinecraftVersion, StringComparison.OrdinalIgnoreCase),
                Category = MinecraftVersionCategory.Custom,
                BadgeIcon = "📦",
                InstancePath = inst.Path
            });
        }

        Versions.Clear();
        if (installed.Count > 0)
        {
            Versions.Add(new MinecraftVersionInfo { IsGroupHeader = true, Id = "Installed" });
            foreach (var v in installed) Versions.Add(v);
        }
        if (custom.Count > 0)
        {
            Versions.Add(new MinecraftVersionInfo { IsGroupHeader = true, Id = "Custom" });
            foreach (var v in custom) Versions.Add(v);
        }
        if (uninstalled.Count > 0)
        {
            Versions.Add(new MinecraftVersionInfo { IsGroupHeader = true, Id = "Available" });
            foreach (var v in uninstalled) Versions.Add(v);
        }

        SelectedVersion = Versions.FirstOrDefault(v => !v.IsGroupHeader && (v.Id == settings.LastPlayedVersion || (v is { InstancePath: not null } && v.BaseVersion == settings.LastPlayedVersion)))
            ?? Versions.FirstOrDefault(v => v.IsInstalled) ?? Versions.FirstOrDefault(v => !v.IsGroupHeader);
        IsBusy = false;
        ProgressValue = 0;
        ProgressPercent = "0%";
        LastUpdateText = $"Updated {DateTime.Now:dd/MM/yyyy}";
        ApplyReadyHeader();
        UpdateButtonText();
    }

    private static HashSet<MinecraftVersionCategory> GetEnabledCategories(LauncherSettings settings)
    {
        if (settings.EnabledCategories is null || settings.EnabledCategories.Count == 0)
            return new HashSet<MinecraftVersionCategory>((MinecraftVersionCategory[])Enum.GetValues(typeof(MinecraftVersionCategory)));

        var set = new HashSet<MinecraftVersionCategory>();
        foreach (var name in settings.EnabledCategories)
        {
            if (Enum.TryParse<MinecraftVersionCategory>(name, true, out var cat))
                set.Add(cat);
        }
        return set.Count > 0 ? set
            : new HashSet<MinecraftVersionCategory>((MinecraftVersionCategory[])Enum.GetValues(typeof(MinecraftVersionCategory)));
    }

    public System.Collections.ObjectModel.ObservableCollection<MinecraftVersionInfo> Versions { get; } = new();

    private async Task RefreshSkinAsync()
    {
        AvatarImage = await _skinService.GetAvatarAsync(Nickname);
    }

    private void UpdateButtonText()
    {
        if (IsBusy) return;
        if (SelectedVersion is null) { ButtonText = "CHECK"; return; }
        if (SelectedVersion.InstancePath is not null) { ButtonText = LanguageService.Instance.T("play.button.play"); return; }
        if (!string.IsNullOrWhiteSpace(SelectedVersion.Id) && !_minecraft.IsJavaReadyFor(SelectedVersion.Id))
        {
            ButtonText = LanguageService.Instance.T("play.button.install_java");
            return;
        }
        ButtonText = string.IsNullOrWhiteSpace(SelectedVersion.Id)
            ? "CHECK"
            : (_minecraft.IsVersionInstalled(SelectedVersion.Id) ? LanguageService.Instance.T("play.button.play") : LanguageService.Instance.T("play.button.install"));
    }

    private void ApplyReadyHeader()
    {
        if (IsBusy) return;
        HeaderSubtitle = SelectedVersion is null ? Nickname : $"{Nickname} • {SelectedVersion.Id}";
        ProgressDetail = string.Empty;
        ProgressSpeed = string.Empty;
        ProgressEta = string.Empty;
    }

    [RelayCommand]
    public async Task PrimaryAction()
    {
        if (SelectedVersion is null) return;

        var settings = _settingsStorage.Load();
        settings.Nickname = Nickname;
        _settingsStorage.Save(settings);

        var versionId = SelectedVersion.InstancePath is not null ? SelectedVersion.BaseVersion : SelectedVersion.Id;

        try
        {
            ButtonText = "WORKING...";
            IsBusy = true;
            var progress = new Progress<DownloadProgressInfo>(ApplyDownloadProgress);
            var process = await _minecraft.PrepareAndLaunchAsync(versionId, Nickname, settings, progress, SelectedVersion.InstancePath);
            if (process is not null)
            {
                settings.LastPlayedVersion = versionId;
                _settingsStorage.Save(settings);
            }
            else
            {
                ProgressDetail = _minecraft.GetStatusMessage();
                System.Console.WriteLine($"[PlayViewModel] Launch failed: {_minecraft.GetStatusMessage()}");
            }
        }
        catch (Exception ex)
        {
            System.Console.WriteLine($"[PlayViewModel] PrimaryAction error: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
            UpdateButtonText();
            DownloadManagerService.Instance.ClearGlobalProgress();
        }
    }

    private void ApplyDownloadProgress(DownloadProgressInfo progress)
    {
        IsBusy = true;
        ProgressValue = Math.Clamp(progress.Percent, 0, 100);
        ProgressPercent = $"{ProgressValue:0}%";
        ProgressDetail = string.IsNullOrWhiteSpace(progress.CurrentFile) ? "Preparing" : progress.CurrentFile;
        ProgressSpeed = progress.BytesPerSecond > 0 ? $"{FormatBytes(progress.BytesPerSecond)}/s" : "Calculating";
        ProgressEta = progress.Eta.HasValue ? FormatEta(progress.Eta.Value) : "---";
        ButtonText = "INSTALLING";
        DownloadManagerService.Instance.ReportGlobalProgress(
            string.IsNullOrWhiteSpace(progress.CurrentFile) ? "Preparando..." : progress.CurrentFile,
            progress.Percent,
            "Baixando");
    }

    private static string FormatBytes(double bytes)
    {
        string[] units = { "B", "KB", "MB", "GB" };
        var value = bytes;
        var unit = 0;
        while (value >= 1024 && unit < units.Length - 1) { value /= 1024; unit++; }
        return $"{value:0.##} {units[unit]}";
    }

    private static string FormatEta(TimeSpan eta) => eta <= TimeSpan.Zero ? "0s" : eta.TotalMinutes >= 1 ? $"{eta.TotalMinutes:0}m" : $"{eta.TotalSeconds:0}s";
}