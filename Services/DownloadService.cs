using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace NexusLauncher.Services;

public class DownloadService
{
    private readonly HttpClient _httpClient = new();

    public async Task<string> DownloadTextAsync(string url)
    {
        var response = await _httpClient.GetAsync(url);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync();
    }

    public async Task DownloadFileAsync(string url, string destinationPath)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(destinationPath)!);
        var data = await _httpClient.GetByteArrayAsync(url);
        await File.WriteAllBytesAsync(destinationPath, data);
    }
}
