using System;
using System.IO;
using System.Text.Json;

namespace NexusLauncher.Storage;

public class SettingsStorage
{
    private readonly string _path;

    public SettingsStorage(string? path = null)
    {
        _path = path ?? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Storage", "LocalConfig.json");
        Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
    }

    public void Save(string nickname)
    {
        File.WriteAllText(_path, JsonSerializer.Serialize(new { nickname }));
    }

    public string LoadNickname()
    {
        if (!File.Exists(_path)) return "Player";
        var json = File.ReadAllText(_path);
        var data = JsonSerializer.Deserialize<SettingsData>(json);
        return data?.Nickname ?? "Player";
    }

    private sealed class SettingsData
    {
        public string? Nickname { get; set; }
    }
}
