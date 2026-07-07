using NexusLauncher.Models;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace NexusLauncher.Services;

public sealed class SearchOptions
{
    public string Query { get; set; } = string.Empty;
    public string ProjectType { get; set; } = "modpack";
    public string Loader { get; set; } = string.Empty;
    public string MinecraftVersion { get; set; } = string.Empty;
    public string Sort { get; set; } = "relevance";
    public int Offset { get; set; }
}

public sealed class SearchResult
{
    public IReadOnlyList<ProjectItem> Results { get; init; } = Array.Empty<ProjectItem>();
    public int TotalCount { get; init; }
}

public class ModrinthService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        NumberHandling = JsonNumberHandling.AllowReadingFromString | JsonNumberHandling.WriteAsString,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private readonly HttpClient _httpClient;

    public ModrinthService(HttpClient httpClient)
    {
        _httpClient = httpClient;
        _httpClient.BaseAddress = new Uri("https://api.modrinth.com/v2/");
        _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("NexusLauncher/0.1");
        _httpClient.Timeout = TimeSpan.FromSeconds(30);
    }

    public async Task<SearchResult> SearchAsync(
        SearchOptions options,
        CancellationToken cancellationToken = default)
    {
        var facets = BuildFacets(options);

        var url =
            $"search?limit=30" +
            $"&offset={options.Offset}" +
            $"&index={Uri.EscapeDataString(options.Sort)}" +
            $"&query={Uri.EscapeDataString(options.Query ?? string.Empty)}";

        if (!string.IsNullOrWhiteSpace(facets))
            url += $"&facets={Uri.EscapeDataString(facets)}";

        using var response = await _httpClient.GetAsync(url, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new HttpRequestException($"Modrinth search error: {(int)response.StatusCode} - {error}");
        }

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);

        var result = await JsonSerializer.DeserializeAsync<SearchResponse>(
            stream, JsonOptions, cancellationToken);

        if (result is null)
            throw new Exception("Failed to deserialize Modrinth search response.");

        return new SearchResult
        {
            Results = result.Hits,
            TotalCount = result.TotalHits
        };
    }

    public async Task<List<ModrinthVersion>> GetVersionsAsync(
        string projectId,
        CancellationToken cancellationToken = default)
    {
        using var response = await _httpClient.GetAsync($"project/{projectId}/version", cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new HttpRequestException($"Modrinth version error: {(int)response.StatusCode} - {error}");
        }

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);

        var result = await JsonSerializer.DeserializeAsync<List<ModrinthVersion>>(
            stream, JsonOptions, cancellationToken);

        return result ?? new List<ModrinthVersion>();
    }

    public async Task DownloadFileAsync(
        string url,
        string outputPath,
        IProgress<double>? progress = null,
        CancellationToken cancellationToken = default)
    {
        using var response = await _httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new HttpRequestException($"Download error: {(int)response.StatusCode} - {error}");
        }

        var total = response.Content.Headers.ContentLength ?? -1L;
        var canReport = total > 0 && progress != null;

        await using var input = await response.Content.ReadAsStreamAsync(cancellationToken);
        await using var output = System.IO.File.Create(outputPath);

        var buffer = new byte[81920];
        long readTotal = 0;
        int read;

        while ((read = await input.ReadAsync(buffer.AsMemory(0, buffer.Length), cancellationToken)) > 0)
        {
            await output.WriteAsync(buffer.AsMemory(0, read), cancellationToken);

            readTotal += read;

            if (canReport)
                progress!.Report((double)readTotal / total);
        }
    }

    private static string BuildFacets(SearchOptions options)
    {
        var facets = new List<List<string>>();

        if (!string.IsNullOrWhiteSpace(options.ProjectType))
            facets.Add(new List<string> { $"project_type:{options.ProjectType}" });

        if (!string.IsNullOrWhiteSpace(options.Loader))
            facets.Add(new List<string> { $"categories:{options.Loader.ToLowerInvariant()}" });

        if (!string.IsNullOrWhiteSpace(options.MinecraftVersion))
            facets.Add(new List<string> { $"versions:{options.MinecraftVersion}" });

        return facets.Count == 0
            ? string.Empty
            : JsonSerializer.Serialize(facets);
    }

    private sealed class SearchResponse
    {
        public List<ProjectItem> Hits { get; set; } = new();
        [JsonPropertyName("total_hits")]
        public int TotalHits { get; set; }
    }
}

public class ModrinthVersion
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("version_number")]
    public string VersionNumber { get; set; } = string.Empty;

    [JsonPropertyName("files")]
    public List<ModrinthFile> Files { get; set; } = new();

    [JsonPropertyName("game_versions")]
    public List<string> GameVersions { get; set; } = new();

    [JsonPropertyName("loaders")]
    public List<string> Loaders { get; set; } = new();

    [JsonIgnore]
    public string MinecraftVersionText => GameVersions.Count > 0 ? string.Join(", ", GameVersions) : "—";
}

public class ModrinthFile
{
    [JsonPropertyName("url")]
    public string Url { get; set; } = string.Empty;

    [JsonPropertyName("primary")]
    public bool Primary { get; set; }

    [JsonPropertyName("filename")]
    public string Filename { get; set; } = string.Empty;

    [JsonPropertyName("size")]
    public long Size { get; set; }
}