using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NexusLauncher.Models;
using NexusLauncher.Services;
using NexusLauncher.ViewModels.Base;
using System;
using System.Collections.ObjectModel;
using System.Linq;

namespace NexusLauncher.ViewModels;

public partial class SelectInstanceOverlayViewModel : ViewModelBase
{
    private readonly InstanceService _instanceService;

    [ObservableProperty] private bool visible;
    [ObservableProperty] private ProjectItem? project;
    [ObservableProperty] private MinecraftInstance? selectedInstance;
    [ObservableProperty] private string? statusText;

    public ObservableCollection<MinecraftInstance> Instances { get; } = new();

    public string TitleLabel => LanguageService.Instance.T("install.select_instance.title");
    public string SubtitleLabel => LanguageService.Instance.T("install.select_instance.subtitle");
    public string ConfirmLabel => LanguageService.Instance.T("install.button.install");
    public string CancelLabel => LanguageService.Instance.T("install.button.cancel");
    public string NoInstancesLabel => LanguageService.Instance.T("install.select_instance.no_instances");

    public IRelayCommand SelectCommand { get; }
    public IRelayCommand CancelCommand { get; }

    public event Action? Confirmed;
    public event Action? Closed;

    public SelectInstanceOverlayViewModel(InstanceService instanceService)
    {
        _instanceService = instanceService;
        SelectCommand = new RelayCommand(SelectAction);
        CancelCommand = new RelayCommand(CancelAction);

        LanguageService.Instance.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName is null || e.PropertyName == nameof(LanguageService.Instance.Current))
            {
                OnPropertyChanged(nameof(TitleLabel));
                OnPropertyChanged(nameof(SubtitleLabel));
                OnPropertyChanged(nameof(ConfirmLabel));
                OnPropertyChanged(nameof(CancelLabel));
                OnPropertyChanged(nameof(NoInstancesLabel));
            }
        };
    }

    public void Initialize(ProjectItem project)
    {
        Project = project;
        StatusText = project.Title;
        SelectedInstance = null;

        Instances.Clear();
        foreach (var inst in _instanceService.GetInstances().OrderBy(i => i.Name))
            Instances.Add(inst);

        Visible = true;
    }

    private void SelectAction()
    {
        if (SelectedInstance is null) return;
        Visible = false;
        Confirmed?.Invoke();
    }

    private void CancelAction()
    {
        Visible = false;
        Closed?.Invoke();
    }
}
