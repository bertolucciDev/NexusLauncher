using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using NexusLauncher.Models;

namespace NexusLauncher.Services;

public enum DownloadStatus
{
    Pending,
    Downloading,
    Completed,
    Failed,
    Paused
}

public partial class DownloadTask : ObservableObject
{
    public string Id { get; } = Guid.NewGuid().ToString();
    public string FileName { get; set; } = string.Empty;
    
    [ObservableProperty] private double _percent;
    [ObservableProperty] private string _status = "Pendente";
    [ObservableProperty] private string _speed = "0 KB/s";
    [ObservableProperty] private DownloadStatus _state = DownloadStatus.Pending;
    
    public CancellationTokenSource Cts { get; set; } = new();
}

public partial class DownloadManagerService : ObservableObject
{
    private static readonly Lazy<DownloadManagerService> _instance = new(() => new DownloadManagerService());
    public static DownloadManagerService Instance => _instance.Value;

    public ObservableCollection<DownloadTask> Queue { get; } = new();
    private readonly SemaphoreSlim _semaphore = new(3);

    private DownloadManagerService() { }

    [ObservableProperty] private bool _globalBusy;
    [ObservableProperty] private double _globalProgress;
    [ObservableProperty] private string _globalText = "";
    [ObservableProperty] private string _globalFile = "";

    public async Task EnqueueAsync(string fileName, Func<DownloadTask, CancellationToken, Task> downloadAction)
    {
        var task = new DownloadTask { FileName = fileName, State = DownloadStatus.Pending };
        Queue.Add(task);

        await Task.Run(async () =>
        {
            try
            {
                await _semaphore.WaitAsync();
                task.State = DownloadStatus.Downloading;
                task.Status = "Baixando...";
                
                await downloadAction(task, task.Cts.Token);
                
                task.State = DownloadStatus.Completed;
                task.Status = "Concluído";
                task.Percent = 100;
            }
            catch (OperationCanceledException)
            {
                task.State = DownloadStatus.Paused;
                task.Status = "Pausado";
            }
            catch (Exception ex)
            {
                task.State = DownloadStatus.Failed;
                task.Status = $"Erro: {ex.Message}";
            }
            finally
            {
                _semaphore.Release();
            }
        });
    }

    public void CancelTask(DownloadTask task)
    {
        task.Cts.Cancel();
    }

    public void ReportGlobalProgress(string file, double percent, string text)
    {
        GlobalBusy = true;
        GlobalFile = file;
        GlobalProgress = percent;
        GlobalText = text;
    }

    public void ClearGlobalProgress()
    {
        GlobalBusy = false;
        GlobalProgress = 0;
        GlobalFile = "";
        GlobalText = "";
    }
}