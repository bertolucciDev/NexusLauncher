using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace NexusLauncher.Models;

public sealed class ModrinthProject
{
    [JsonPropertyName("project_id")]
    public string ProjectId { get; set; } = string.Empty;
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
    public List<string> Categories { get; set; } = new();
    public List<string> Versions { get; set; } = new();
    public string LoaderText => Categories.Count == 0 ? "—" : string.Join(", ", Categories.FindAll(IsLoader));
    public string VersionText => string.IsNullOrWhiteSpace(LatestVersion) ? "Todas" : LatestVersion;

    private static bool IsLoader(string value) => value is "fabric" or "forge" or "neoforge" or "quilt";
}
