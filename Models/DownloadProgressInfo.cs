using System;

namespace NexusLauncher.Models;

public sealed class DownloadProgressInfo
{
    public long BytesDownloaded { get; init; }
    public long? TotalBytes { get; init; }
    public double Percent => TotalBytes is > 0 ? BytesDownloaded * 100d / TotalBytes.Value : 0d;
    public double BytesPerSecond { get; init; }
    public TimeSpan? Eta { get; init; }
    public string CurrentFile { get; init; } = string.Empty;
    public string State { get; init; } = "Baixando";
}
