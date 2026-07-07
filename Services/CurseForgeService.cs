using NexusLauncher.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace NexusLauncher.Services;

public sealed class CurseForgeService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        NumberHandling = JsonNumberHandling.AllowReadingFromString | JsonNumberHandling.WriteAsString,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private readonly HttpClient _httpClient;

    private static readonly Dictionary<string, int> ClassMap = new()
    {
        ["modpack"] = 4471,
        ["mod"] = 6,
        ["resourcepack"] = 12,
        ["shader"] = 6552,
    };

    private static readonly Dictionary<string, int> SortFieldMap = new()
    {
        ["relevance"] = 1,  // Featured
        ["downloads"] = 6,  // Total Downloads
        ["follows"] = 6,    // Total Downloads (CF has no "follows")
        ["updated"] = 3,    // Last Updated
        ["newest"] = 3,     // Last Updated (CF has no "date created")
    };

    public CurseForgeService(HttpClient httpClient)
    {
        _httpClient = httpClient;
        _httpClient.Timeout = TimeSpan.FromSeconds(30);
    }

    public async Task<SearchResult> SearchAsync(
        SearchOptions options,
        CancellationToken cancellationToken = default)
    {
        var apiKey = LauncherRuntime.Settings.Load().CurseForgeApiKey.Trim();
        if (string.IsNullOrWhiteSpace(apiKey))
            throw new InvalidOperationException("CurseForge API key nao configurada. Adicione em Configuracoes.");

        var classId = ClassMap.GetValueOrDefault(options.ProjectType ?? "", 0);
        var sortField = SortFieldMap.GetValueOrDefault(options.Sort, 1);

        var queryParams = new List<string>
        {
            "gameId=432",
            $"index={options.Offset}",
            $"sortField={sortField}",
            "sortOrder=desc",
            "pageSize=30"
        };

        if (!string.IsNullOrWhiteSpace(options.Query))
            queryParams.Add($"searchFilter={Uri.EscapeDataString(options.Query)}");

        if (classId > 0)
            queryParams.Add($"classId={classId}");

        var url = $"https://api.curseforge.com/v1/mods/search?" + string.Join("&", queryParams);

        System.Console.WriteLine($"[CurseForge] URL: {url}");

        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Add("x-api-key", apiKey);
        request.Headers.UserAgent.ParseAdd("NexusLauncher/0.1");

        using var response = await _httpClient.SendAsync(request, cancellationToken);

if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync(cancellationToken);
            System.Console.WriteLine($"[CurseForge] HTTP {(int)response.StatusCode}: {error}");
            var msg = $"CurseForge {(int)response.StatusCode}";
            if ((int)response.StatusCode == 404)
                msg += " - API key pode nao ter acesso ao gameId 432 (Minecraft). Solicite em https://console.curseforge.com";
            else if (!string.IsNullOrWhiteSpace(error))
                msg += $": {error.Trim()}";
            throw new HttpRequestException(msg);
        }

        System.Console.WriteLine($"[CurseForge] OK - buscando mods");

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);

        var result = await JsonSerializer.DeserializeAsync<CurseForgeListResponse<CurseForgeMod>>(
            stream, JsonOptions, cancellationToken);

        if (result?.Data is null)
            return new SearchResult();

        return new SearchResult
        {
            Results = MapResults(result.Data, options),
            TotalCount = result.Pagination?.TotalCount ?? result.Data.Count
        };
    }

    public async Task<CurseForgeFile?> GetLatestFileAsync(
        int modId,
        CancellationToken cancellationToken = default)
    {
        var apiKey = LauncherRuntime.Settings.Load().CurseForgeApiKey.Trim();
        if (string.IsNullOrWhiteSpace(apiKey))
            return null;

        var url = $"https://api.curseforge.com/v1/mods/{modId}/files?gameId=432&pageSize=1&index=0";

        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Add("x-api-key", apiKey);
        request.Headers.Accept.ParseAdd("application/json");
        request.Headers.UserAgent.ParseAdd("NexusLauncher/0.1");

        using var response = await _httpClient.SendAsync(request, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            System.Console.WriteLine($"[CurseForge] GetLatestFile(modId={modId}) HTTP {(int)response.StatusCode}: {body}");
            return null;
        }

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);

        var result = await JsonSerializer.DeserializeAsync<CurseForgeListResponse<CurseForgeFile>>(
            stream, JsonOptions, cancellationToken);

        var file = result?.Data?.Count > 0 ? result.Data[0] : null;

        if (file is not null && string.IsNullOrWhiteSpace(file.DownloadUrl))
        {
            System.Console.WriteLine($"[CurseForge] GetLatestFile(modId={modId}) downloadUrl vazio, fileId={file.Id}");
        }

        return file;
    }

    public async Task<string?> GetDownloadUrlAsync(
        int modId,
        int fileId,
        CancellationToken cancellationToken = default)
    {
        var apiKey = LauncherRuntime.Settings.Load().CurseForgeApiKey.Trim();
        if (string.IsNullOrWhiteSpace(apiKey))
            return null;

        var url = $"https://api.curseforge.com/v1/mods/{modId}/files/{fileId}/download";

        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Add("x-api-key", apiKey);
        request.Headers.Accept.ParseAdd("application/json");
        request.Headers.UserAgent.ParseAdd("NexusLauncher/0.1");

        using var response = await _httpClient.SendAsync(request, cancellationToken);

        if ((int)response.StatusCode is >= 300 and < 400)
        {
            var location = response.Headers.Location?.ToString();
            if (!string.IsNullOrWhiteSpace(location))
            {
                System.Console.WriteLine($"[CurseForge] Download redirect for mod={modId}, file={fileId}: {location}");
                return location;
            }
        }

        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            System.Console.WriteLine($"[CurseForge] GetDownloadUrl(mod={modId}, file={fileId}) HTTP {(int)response.StatusCode}: {body}");
        }

        return null;
    }

    private static List<ProjectItem> MapResults(List<CurseForgeMod> mods, SearchOptions options)
    {
        var results = new List<ProjectItem>(mods.Count);
        foreach (var mod in mods)
        {
            var categories = new List<string>();
            if (mod.Categories is not null)
                foreach (var cat in mod.Categories)
                    if (!string.IsNullOrWhiteSpace(cat.Name))
                        categories.Add(cat.Name.ToLowerInvariant());

            results.Add(new ProjectItem
            {
                Source = "CurseForge",
                CurseForgeId = mod.Id,
                Title = mod.Name ?? "?",
                Slug = mod.Slug ?? "",
                Author = mod.Authors is { Count: > 0 } ? mod.Authors[0].Name ?? "?" : "?",
                Description = mod.Summary ?? "",
                ProjectType = options.ProjectType ?? "mod",
                IconUrl = mod.Logo?.Url,
                Downloads = mod.DownloadCount,
                Follows = mod.DownloadCount,
                LatestVersion = mod.DateModified?.ToString("yyyy-MM-dd") ?? "",
                Categories = categories,
            });
        }
        return results;
    }

    private sealed class CurseForgeListResponse<T>
    {
        [JsonPropertyName("data")]
        public List<T>? Data { get; set; }

        [JsonPropertyName("pagination")]
        public CurseForgePagination? Pagination { get; set; }
    }

    private sealed class CurseForgePagination
    {
        [JsonPropertyName("totalCount")]
        public int TotalCount { get; set; }
    }

    private sealed class CurseForgeMod
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("slug")]
        public string? Slug { get; set; }

        [JsonPropertyName("summary")]
        public string? Summary { get; set; }

        [JsonPropertyName("links")]
        public CurseForgeLinks? Links { get; set; }

        [JsonPropertyName("downloadCount")]
        public int DownloadCount { get; set; }

        [JsonPropertyName("dateModified")]
        public DateTime? DateModified { get; set; }

        [JsonPropertyName("logo")]
        public CurseForgeLogo? Logo { get; set; }

        [JsonPropertyName("categories")]
        public List<CurseForgeCategory>? Categories { get; set; }

        [JsonPropertyName("authors")]
        public List<CurseForgeAuthor>? Authors { get; set; }
    }

    private sealed class CurseForgeLinks
    {
        [JsonPropertyName("websiteUrl")]
        public string? WebsiteUrl { get; set; }
    }

    private sealed class CurseForgeLogo
    {
        [JsonPropertyName("url")]
        public string? Url { get; set; }
    }

    private sealed class CurseForgeCategory
    {
        [JsonPropertyName("name")]
        public string? Name { get; set; }
    }

    private sealed class CurseForgeAuthor
    {
        [JsonPropertyName("name")]
        public string? Name { get; set; }
    }

    public sealed class CurseForgeFile
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("fileName")]
        public string FileName { get; set; } = "";

        [JsonPropertyName("downloadUrl")]
        public string? DownloadUrl { get; set; }
    }
}
