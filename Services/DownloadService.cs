using NexusLauncher.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace NexusLauncher.Services;

public class DownloadService
{
    private readonly HttpClient _httpClient = new();

    public async Task<string> DownloadTextAsync(string url, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetAsync(url, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync(cancellationToken);
    }

    public async Task DownloadFileAsync(string url, string destinationPath, IProgress<DownloadProgressInfo>? progress = null, CancellationToken cancellationToken = default)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(destinationPath)!);
        using var response = await _httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        response.EnsureSuccessStatusCode();

        var totalBytes = response.Content.Headers.ContentLength;
        await using var input = await response.Content.ReadAsStreamAsync(cancellationToken);
        await using var output = File.Create(destinationPath);

        var buffer = new byte[81920];
        var downloaded = 0L;
        var samples = new Queue<(TimeSpan Time, long Bytes)>();
        var clock = Stopwatch.StartNew();

        while (true)
        {
            var read = await input.ReadAsync(buffer.AsMemory(0, buffer.Length), cancellationToken);
            if (read == 0) break;

            await output.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
            downloaded += read;
            samples.Enqueue((clock.Elapsed, downloaded));
            while (samples.Count > 2 && clock.Elapsed - samples.Peek().Time > TimeSpan.FromSeconds(8))
                samples.Dequeue();

            var speed = CalculateMovingAverage(samples);
            var remaining = totalBytes.HasValue ? Math.Max(0, totalBytes.Value - downloaded) : 0;
            progress?.Report(new DownloadProgressInfo
            {
                BytesDownloaded = downloaded,
                TotalBytes = totalBytes,
                BytesPerSecond = speed,
                Eta = totalBytes.HasValue && speed > 0 ? TimeSpan.FromSeconds(remaining / speed) : null,
                CurrentFile = Path.GetFileName(destinationPath),
                State = "Baixando"
            });
        }

        progress?.Report(new DownloadProgressInfo
        {
            BytesDownloaded = downloaded,
            TotalBytes = totalBytes ?? downloaded,
            BytesPerSecond = 0,
            Eta = TimeSpan.Zero,
            CurrentFile = Path.GetFileName(destinationPath),
            State = "Concluído"
        });
    }

    private static double CalculateMovingAverage(Queue<(TimeSpan Time, long Bytes)> samples)
    {
        if (samples.Count < 2) return 0;
        var first = samples.Peek();
        var last = default((TimeSpan Time, long Bytes));
        foreach (var sample in samples) last = sample;
        var seconds = (last.Time - first.Time).TotalSeconds;
        return seconds <= 0 ? 0 : (last.Bytes - first.Bytes) / seconds;
    }
}
