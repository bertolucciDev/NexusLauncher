using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using NexusLauncher.Models;

namespace NexusLauncher.Services;

public class InstanceService
{
    private readonly SettingsService _settingsService;

    public InstanceService(SettingsService settingsService)
    {
        _settingsService = settingsService;
    }

    public string InstancesFolder
    {
        get
        {
            var root = _settingsService.Load().MinecraftDirectory;
            var folder = Path.Combine(root, "instances");
            Directory.CreateDirectory(folder);
            return folder;
        }
    }

    public IEnumerable<MinecraftInstance> GetInstances()
    {
        foreach (var dir in Directory.GetDirectories(InstancesFolder))
        {
            var json = Path.Combine(dir, "instance.json");

            if (!File.Exists(json))
                continue;

            var text = File.ReadAllText(json);

            var instance =
                JsonSerializer.Deserialize<MinecraftInstance>(text);

            if (instance != null)
                yield return instance;
        }
    }

    public MinecraftInstance Create(
        string name,
        string mcVersion,
        string loader,
        string loaderVersion)
    {
        var folder = Path.Combine(InstancesFolder, name);

        Directory.CreateDirectory(folder);

        Directory.CreateDirectory(Path.Combine(folder, "mods"));
        Directory.CreateDirectory(Path.Combine(folder, "config"));
        Directory.CreateDirectory(Path.Combine(folder, "resourcepacks"));
        Directory.CreateDirectory(Path.Combine(folder, "shaderpacks"));
        Directory.CreateDirectory(Path.Combine(folder, "saves"));

        var instance = new MinecraftInstance
        {
            Name = name,
            MinecraftVersion = mcVersion,
            Loader = loader,
            LoaderVersion = loaderVersion,
            Path = folder
        };

        File.WriteAllText(
            Path.Combine(folder, "instance.json"),
            JsonSerializer.Serialize(instance,
                new JsonSerializerOptions
                {
                    WriteIndented = true
                }));

        return instance;
    }

    public void Delete(MinecraftInstance instance)
    {
        if (string.IsNullOrWhiteSpace(instance?.Path)) return;
        var instancesRoot = InstancesFolder;
        var fullPath = Path.GetFullPath(instance.Path);
        if (!fullPath.StartsWith(Path.GetFullPath(instancesRoot), StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException($"Path fora do diretorio de instancias: {instance.Path}");
        if (Directory.Exists(fullPath))
            Directory.Delete(fullPath, true);
    }
}