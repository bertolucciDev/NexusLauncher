using NexusLauncher.Models;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace NexusLauncher.Services;

public sealed class ModrinthSearchOptions
{
    public string Query { get; set; } = string.Empty;
    public string ProjectType { get; set; } = "modpack";
    public string Loader { get; set; } = string.Empty;
    public string MinecraftVersion { get; set; } = string.Empty;
    public string Sort { get; set; } = "relevance";
}

public class ModrinthService
{
    private readonly HttpClient _httpClient = new() { BaseAddress = new Uri("https://api.modrinth.com/v2/") };

    public ModrinthService()
    {
        _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("NexusLauncher/0.1 (desktop launcher)");
    }

    public async Task<IReadOnlyList<ModrinthProject>> SearchAsync(ModrinthSearchOptions options, CancellationToken cancellationToken = default)
    {
        var facets = BuildFacets(options);
        var url = $"search?limit=30&index={Uri.EscapeDataString(options.Sort)}&query={Uri.EscapeDataString(options.Query ?? string.Empty)}";
        if (!string.IsNullOrWhiteSpace(facets)) url += $"&facets={Uri.EscapeDataString(facets)}";
        using var response = await _httpClient.GetAsync(url, cancellationToken);
        response.EnsureSuccessStatusCode();
        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        var result = await JsonSerializer.DeserializeAsync<SearchResponse>(stream, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }, cancellationToken);
        return result?.Hits ?? new List<ModrinthProject>();
    }

    private static string BuildFacets(ModrinthSearchOptions options)
    {
        var groups = new List<string>();
        if (!string.IsNullOrWhiteSpace(options.ProjectType)) groups.Add($"[\"project_type:{options.ProjectType}\"]");
        if (!string.IsNullOrWhiteSpace(options.Loader)) groups.Add($"[\"categories:{options.Loader.ToLowerInvariant()}\"]");
        if (!string.IsNullOrWhiteSpace(options.MinecraftVersion)) groups.Add($"[\"versions:{options.MinecraftVersion}\"]");
        return groups.Count == 0 ? string.Empty : $"[{string.Join(',', groups)}]";
    }

    private sealed class SearchResponse
    {
        public List<ModrinthProject> Hits { get; set; } = new();
    }
}
