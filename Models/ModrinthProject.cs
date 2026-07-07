using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace NexusLauncher.Models;

public enum InstallState { Idle, Installing, Installed, Error }

public partial class ProjectItem : ObservableObject
{
    private static readonly HttpClient _iconClient = new();

    [JsonIgnore]
    [ObservableProperty]
    private InstallState installState = InstallState.Idle;

    [JsonIgnore]
    [ObservableProperty]
    private double installProgress;

    [JsonIgnore]
    [ObservableProperty]
    private string installProgressText = string.Empty;

    // Modrinth-specific
    [JsonPropertyName("project_id")]
    public string ProjectId { get; set; } = string.Empty;

    // Common
    public string Slug { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Author { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("project_type")]
    public string ProjectType { get; set; } = string.Empty;

    [JsonPropertyName("icon_url")]
    public string? IconUrl { get; set; }

    public int Downloads { get; set; }
    public int Follows { get; set; }

    [JsonPropertyName("latest_version")]
    public string LatestVersion { get; set; } = string.Empty;

    [JsonPropertyName("categories")]
    public List<string> Categories { get; set; } = new();

    [JsonPropertyName("versions")]
    public List<string> Versions { get; set; } = new();

    // Source tracking
    [JsonIgnore]
    public string Source { get; set; } = "Modrinth";

    // CurseForge-specific
    [JsonIgnore]
    public int CurseForgeId { get; set; }

    [JsonIgnore]
    public string LoaderText => Categories.Count == 0 ? "—" : string.Join(", ", Categories.FindAll(IsLoader));

    [JsonIgnore]
    public string VersionText => string.IsNullOrWhiteSpace(LatestVersion) ? "Todas" : LatestVersion;

    [JsonIgnore]
    [ObservableProperty]
    private Bitmap? icon;

    public async Task LoadIconAsync()
    {
        if (string.IsNullOrWhiteSpace(IconUrl) || Icon is not null) return;
        try
        {
            var bytes = await _iconClient.GetByteArrayAsync(IconUrl);
            using var ms = new MemoryStream(bytes);
            Icon = new Bitmap(ms);
        }
        catch { }
    }

    private static bool IsLoader(string value) => value is "fabric" or "forge" or "neoforge" or "quilt";
}
