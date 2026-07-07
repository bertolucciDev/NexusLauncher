using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NexusLauncher.Models;
using NexusLauncher.Services;
using NexusLauncher.ViewModels.Base;
using System;
using System.Collections.ObjectModel;
using System.Linq;

namespace NexusLauncher.ViewModels;

public partial class InstancesViewModel : ViewModelBase
{
    private readonly InstanceService _instanceService;
    private readonly SettingsService _settingsService;
    private readonly CreateInstanceOverlayViewModel _overlay;

    [ObservableProperty] private string searchText = string.Empty;
    [ObservableProperty] private bool hasInstances = true;
    [ObservableProperty] private bool hasNoInstances;

    public ObservableCollection<MinecraftInstance> FilteredInstances { get; } = new();
    private readonly ObservableCollection<MinecraftInstance> _all = new();

    public string TitleLabel => LanguageService.Instance.T("instances.title");
    public string SubtitleLabel => LanguageService.Instance.T("instances.subtitle");
    public string SearchPlaceholder => LanguageService.Instance.T("instances.search.placeholder");
    public string NewButtonLabel => LanguageService.Instance.T("instances.button.new");
    public string PlayButtonLabel => LanguageService.Instance.T("instances.action.play");
    public string RemoveButtonLabel => LanguageService.Instance.T("instances.action.remove");
    public string EmptyMessage => LanguageService.Instance.T("instances.empty");
    public string EmptyAction => LanguageService.Instance.T("instances.empty_action");

    public IRelayCommand CreateInstanceCommand { get; }
    public IRelayCommand PlayInstanceCommand { get; }
    public IRelayCommand DeleteInstanceCommand { get; }

    public InstancesViewModel(CreateInstanceOverlayViewModel overlay)
    {
        _instanceService = new InstanceService(new SettingsService());
        _settingsService = new SettingsService();
        _overlay = overlay;

        CreateInstanceCommand = new RelayCommand(() => _overlay.Initialize());
        PlayInstanceCommand = new RelayCommand<MinecraftInstance>(Play);
        DeleteInstanceCommand = new RelayCommand<MinecraftInstance>(Delete);

        LanguageService.Instance.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName is null || e.PropertyName == nameof(LanguageService.Instance.Current))
            {
                OnPropertyChanged(nameof(TitleLabel));
                OnPropertyChanged(nameof(SubtitleLabel));
                OnPropertyChanged(nameof(SearchPlaceholder));
                OnPropertyChanged(nameof(NewButtonLabel));
                OnPropertyChanged(nameof(PlayButtonLabel));
                OnPropertyChanged(nameof(RemoveButtonLabel));
                OnPropertyChanged(nameof(EmptyMessage));
                OnPropertyChanged(nameof(EmptyAction));
            }
        };

        _overlay.Closed += () => Refresh();
        Refresh();
    }

    public void Refresh()
    {
        _all.Clear();
        foreach (var i in _instanceService.GetInstances()) _all.Add(i);
        ApplyFilter();
    }

    partial void OnSearchTextChanged(string value) => ApplyFilter();

    private void ApplyFilter()
    {
        FilteredInstances.Clear();
        var q = string.IsNullOrWhiteSpace(SearchText)
            ? _all
            : _all.Where(i => i.Name.Contains(SearchText, StringComparison.OrdinalIgnoreCase));
        foreach (var i in q) FilteredInstances.Add(i);
        HasInstances = FilteredInstances.Count > 0;
        HasNoInstances = FilteredInstances.Count == 0;
    }

    private void Play(MinecraftInstance? instance)
    {
        if (instance is null) return;
        var settings = _settingsService.Load();
        settings.LastPlayedVersion = instance.MinecraftVersion;
        _settingsService.Save(settings);
        NavigationService.Instance.NavigateTo(new PlayViewModel());
    }

    private void Delete(MinecraftInstance? instance)
    {
        if (instance is null) return;
        try { _instanceService.Delete(instance); Refresh(); }
        catch (Exception ex) { Console.WriteLine($"Delete error: {ex.Message}"); }
    }
}