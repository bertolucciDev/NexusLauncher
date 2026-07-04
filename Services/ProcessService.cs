using NexusLauncher.Models;
using System;
using System.Diagnostics;

namespace NexusLauncher.Services;

public class ProcessService
{
    public Process? CurrentMinecraftProcess { get; private set; }
    public int? CurrentProcessId => CurrentMinecraftProcess?.HasExited == false ? CurrentMinecraftProcess.Id : null;
    public bool IsGameRunning => CurrentMinecraftProcess?.HasExited == false;

    public event EventHandler? MinecraftStarted;
    public event EventHandler? MinecraftExited;

    public void Track(Process process, LauncherSettings settings)
    {
        CurrentMinecraftProcess = process;
        process.EnableRaisingEvents = true;
        process.Exited += (_, _) =>
        {
            CurrentMinecraftProcess = null;
            MinecraftExited?.Invoke(this, EventArgs.Empty);
        };

        MinecraftStarted?.Invoke(this, EventArgs.Empty);
    }
}
