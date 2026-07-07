using NexusLauncher.Models;
using System;
using System.Diagnostics;

namespace NexusLauncher.Services;

public class ProcessService
{
    private Process? _currentMinecraftProcess;
    private readonly object _lock = new();

    public Process? CurrentMinecraftProcess
    {
        get { lock (_lock) return _currentMinecraftProcess; }
        private set { lock (_lock) _currentMinecraftProcess = value; }
    }

    public int? CurrentProcessId
    {
        get
        {
            lock (_lock)
                return _currentMinecraftProcess?.HasExited == false ? _currentMinecraftProcess.Id : null;
        }
    }

    public bool IsGameRunning
    {
        get
        {
            lock (_lock)
                return _currentMinecraftProcess?.HasExited == false;
        }
    }

    public event EventHandler? MinecraftStarted;
    public event EventHandler? MinecraftExited;

    public void Track(Process process, LauncherSettings settings)
    {
        var old = CurrentMinecraftProcess;
        if (old is not null && !old.HasExited)
        {
            try { old.Exited -= HandleProcessExited; } catch { }
        }

        CurrentMinecraftProcess = process;
        process.EnableRaisingEvents = true;
        process.Exited += HandleProcessExited;

        MinecraftStarted?.Invoke(this, EventArgs.Empty);
    }

    private void HandleProcessExited(object? sender, EventArgs e)
    {
        Avalonia.Threading.Dispatcher.UIThread.Post(() =>
        {
            CurrentMinecraftProcess = null;
            MinecraftExited?.Invoke(this, EventArgs.Empty);
        });
    }
}
