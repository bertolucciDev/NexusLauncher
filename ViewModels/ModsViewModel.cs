using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NexusLauncher.Models;
using NexusLauncher.Services;
using NexusLauncher.ViewModels.Base;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace NexusLauncher.ViewModels;

public partial class ModsViewModel : ViewModelBase
{
    private readonly ModrinthService _modrinthService = new();

    [ObservableProperty] private string searchText = string.Empty;
    [ObservableProperty] private string selectedProjectType = "modpack";
    [ObservableProperty] private string selectedLoader = string.Empty;
    [ObservableProperty] private string minecraftVersion = string.Empty;
    [ObservableProperty] private string selectedSort = "relevance";
    [ObservableProperty] private string statusText = "Explore Modrinth";
    [ObservableProperty] private bool isBusy;

    public ObservableCollection<string> ProjectTypes { get; } = new() { "modpack", "mod", "resourcepack", "shader" };
    public ObservableCollection<string> Loaders { get; } = new() { string.Empty, "fabric", "forge", "neoforge", "quilt" };
    public ObservableCollection<string> Sorts { get; } = new() { "relevance", "downloads", "follows", "updated", "newest" };
    public ObservableCollection<ModrinthProject> Projects { get; } = new();

    public ModsViewModel() => _ = SearchAsync();

    [RelayCommand]
    private async Task SearchAsync()
    {
        IsBusy = true;
        StatusText = "Buscando projetos no Modrinth...";
        try
        {
            var results = await _modrinthService.SearchAsync(new ModrinthSearchOptions
            {
                Query = SearchText,
                ProjectType = SelectedProjectType,
                Loader = SelectedLoader,
                MinecraftVersion = MinecraftVersion,
                Sort = SelectedSort
            });
            Projects.Clear();
            foreach (var project in results) Projects.Add(project);
            StatusText = $"{Projects.Count} resultados encontrados";
        }
        catch
        {
            StatusText = "Não foi possível carregar o Modrinth agora.";
        }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    private void Install(ModrinthProject project)
    {
        StatusText = project.ProjectType == "modpack"
            ? $"Instalação do modpack {project.Title} será enfileirada."
            : $"Escolha uma instalação para receber {project.Title}.";
    }
}
