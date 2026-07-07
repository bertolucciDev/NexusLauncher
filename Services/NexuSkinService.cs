using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace NexusLauncher.Services;

public class NexuSkinService
{
    private const string AGENT_URL =
        "https://github.com/bertolucciDev/NexuSkin/releases/download/v1.2.0/nxagent.jar";

    private static readonly HttpClient Http = new() { Timeout = TimeSpan.FromSeconds(20) };

    public string AgentJarPath { get; }
    public string CacheDir { get; }

    private readonly ConcurrentDictionary<string, byte> _prefetched = new(StringComparer.OrdinalIgnoreCase);

    public NexuSkinService()
    {
        var dataRoot = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "NexusLauncher");

        Directory.CreateDirectory(dataRoot);
        AgentJarPath = Path.Combine(dataRoot, "nxagent.jar");
        CacheDir = Path.Combine(dataRoot, "NexuSkinCache");
        Directory.CreateDirectory(CacheDir);
    }

    public async Task EnsureAgentAsync(System.IProgress<string>? progress = null)
    {
        if (File.Exists(AgentJarPath)) return;
        progress?.Report("NexuSkin: baixando agente...");
        await DownloadAsync(AGENT_URL, AgentJarPath);
    }

    public async Task PrepareForUserAsync(string username)
    {
        if (string.IsNullOrWhiteSpace(username)) return;
        await PrefetchByUsernameAsync(username);
    }

    public async Task PrefetchByUsernameAsync(string username)
    {
        if (string.IsNullOrWhiteSpace(username)) return;
        var key = username.Trim().ToLowerInvariant();
        if (!_prefetched.TryAdd(key, 0)) return;
        try
        {
            var uuid = await FetchUuidAsync(username);
            if (string.IsNullOrWhiteSpace(uuid)) return;
            await DownloadIfMissingAsync($"https://crafatar.com/skins/{uuid}",
                Path.Combine(CacheDir, key + "_skin.png"));
            try
            {
                await DownloadIfMissingAsync($"https://crafatar.com/capes/{uuid}",
                    Path.Combine(CacheDir, key + "_cape.png"));
            }
            catch { /* optional */ }
        }
        catch { /* silent */ }
    }

    public async Task PrefetchManyAsync(IEnumerable<string?> usernames)
    {
        foreach (var n in usernames)
            await PrefetchByUsernameAsync(n ?? "");
    }

    private async Task DownloadIfMissingAsync(string url, string dest)
    {
        if (File.Exists(dest) && new FileInfo(dest).Length > 0) return;
        await DownloadAsync(url, dest);
    }

    private static async Task DownloadAsync(string url, string dest)
    {
        using var resp = await Http.GetAsync(url);
        resp.EnsureSuccessStatusCode();
        await using var s = await resp.Content.ReadAsStreamAsync();
        await using var f = File.Create(dest);
        await s.CopyToAsync(f);
    }

    private static async Task<string?> FetchUuidAsync(string name)
    {
        try
        {
            var url = $"https://api.mojang.com/users/profiles/minecraft/{Uri.EscapeDataString(name)}";
            using var resp = await Http.GetAsync(url);
            if (!resp.IsSuccessStatusCode) return null;
            var json = await resp.Content.ReadAsStringAsync();
            var marker = "\"id\":\"";
            var i = json.IndexOf(marker, StringComparison.Ordinal);
            if (i < 0) return null;
            var s = i + marker.Length;
            var e = json.IndexOf('"', s);
            if (e < 0) return null;
            var id = json.Substring(s, e - s);
            return id.Insert(8, "-").Insert(13, "-").Insert(18, "-").Insert(23, "-");
        }
        catch { return null; }
    }
}
