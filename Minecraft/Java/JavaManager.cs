using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace NexusLauncher.Minecraft.Java;

public sealed record JavaInstallation(string Path, int MajorVersion);

public class JavaManager
{
    private static readonly HttpClient _http = new();
    private static string JavaStorage => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Minecraft", "Java");

    public string? FindJavaPath(int minimumMajor = 17) => FindBestJava(minimumMajor)?.Path;

    public JavaInstallation? FindBestJava(int minimumMajor = 17)
    {
        var javas = FindInstalledJavas().Concat(FindLocalJavas()).ToList();
        javas.Sort((a, b) => a.MajorVersion.CompareTo(b.MajorVersion));
        return javas.FirstOrDefault(j => j.MajorVersion == minimumMajor)
            ?? javas.Where(j => j.MajorVersion >= minimumMajor).OrderBy(j => j.MajorVersion).FirstOrDefault();
    }

    public IReadOnlyList<JavaInstallation> FindInstalledJavas()
    {
        var candidates = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        AddCandidate(candidates, Environment.GetEnvironmentVariable("JAVA_HOME"));
        foreach (var pathValue in Environment.GetEnvironmentVariable("Path")?.Split(Path.PathSeparator) ?? Array.Empty<string>())
            AddCandidate(candidates, pathValue, false);

        foreach (var root in GetKnownJavaRoots())
        {
            if (!Directory.Exists(root)) continue;
            try
            {
                foreach (var java in Directory.EnumerateFiles(root, OperatingSystem.IsWindows() ? "java.exe" : "java", SearchOption.AllDirectories).Take(80))
                    candidates.Add(java);
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"[JavaManager] FindInstalledJavas: {ex.Message}");
            }
        }

        return candidates.Select(path => new JavaInstallation(path, GetMajorVersion(path))).Where(j => j.MajorVersion > 0)
            .GroupBy(j => j.Path, StringComparer.OrdinalIgnoreCase).Select(g => g.First()).OrderBy(j => j.MajorVersion).ToList();
    }

    private List<JavaInstallation> FindLocalJavas()
    {
        if (!Directory.Exists(JavaStorage)) return new();
        return Directory.EnumerateFiles(JavaStorage, OperatingSystem.IsWindows() ? "java.exe" : "java", SearchOption.AllDirectories)
            .Select(path => new JavaInstallation(path, GetMajorVersion(path)))
            .Where(j => j.MajorVersion > 0).ToList();
    }

    public async Task<string?> EnsureJavaAsync(int majorVersion)
    {
        var existing = FindBestJava(majorVersion);
        if (existing is not null) return existing.Path;

        return await DownloadJavaAsync(majorVersion);
    }

    public async Task<string?> DownloadJavaAsync(int majorVersion)
    {
        try
        {
            var url = $"https://api.adoptium.net/v3/binary/latest/{majorVersion}/ga/windows/x64/jdk/hotspot/normal/eclipse";

            using var response = await _http.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
            response.EnsureSuccessStatusCode();

            var destDir = Path.Combine(JavaStorage, $"jdk-{majorVersion}");
            if (Directory.Exists(destDir)) Directory.Delete(destDir, true);
            Directory.CreateDirectory(destDir);

            var zipPath = Path.Combine(JavaStorage, $"jdk-{majorVersion}.zip");
            using (var fs = new FileStream(zipPath, FileMode.Create, FileAccess.Write, FileShare.None))
                await response.Content.CopyToAsync(fs);

            ZipFile.ExtractToDirectory(zipPath, destDir);

            try { File.Delete(zipPath); } catch { }

            var javaExe = Directory.EnumerateFiles(destDir, "java.exe", SearchOption.AllDirectories).FirstOrDefault();
            if (javaExe is not null && GetMajorVersion(javaExe) >= majorVersion)
                return javaExe;

            return null;
        }
        catch (Exception ex)
        {
            System.Console.WriteLine($"[JavaManager] DownloadJava: {ex.Message}");
            return null;
        }
    }

    public bool IsJava17OrHigher(string? javaPath) => GetMajorVersion(javaPath) >= 17;

    public int GetMajorVersion(string? javaPath)
    {
        if (string.IsNullOrWhiteSpace(javaPath) || !File.Exists(javaPath)) return 0;
        try
        {
            using var process = new Process { StartInfo = new ProcessStartInfo { FileName = javaPath, Arguments = "-version", RedirectStandardError = true, RedirectStandardOutput = true, UseShellExecute = false, CreateNoWindow = true } };
            process.Start();
            var output = process.StandardError.ReadToEnd() + process.StandardOutput.ReadToEnd();
            process.WaitForExit(5000);
            return ParseMajorVersion(output);
        }
        catch (Exception ex) { System.Console.WriteLine($"[JavaManager] GetMajorVersion: {ex.Message}"); return 0; }
    }

    public bool IsJavaVersionSupported(string versionOutput) => ParseMajorVersion(versionOutput) >= 17;

    public static int ParseMajorVersion(string versionOutput)
    {
        if (string.IsNullOrWhiteSpace(versionOutput)) return 0;
        
        // Tenta pegar o padrão "version \"17.0.1\""
        var match = Regex.Match(versionOutput, "version\\s+\\\"(?<version>[^\\\"]+)\\\"");
        if (match.Success)
        {
            var tokens = match.Groups["version"].Value.Split(['.', '_', '-'], StringSplitOptions.RemoveEmptyEntries);
            if (tokens.Length > 0 && int.TryParse(tokens[0], out var major))
            {
                if (major == 1 && tokens.Length > 1 && int.TryParse(tokens[1], out var legacy)) return legacy;
                return major;
            }
        }

        // Fallback: Tenta pegar qualquer número que pareça uma versão no início da string (ex: "17.0.1")
        var fallbackMatch = Regex.Match(versionOutput, @"\d+(\.\d+)*");
        if (fallbackMatch.Success)
        {
            var tokens = fallbackMatch.Value.Split('.');
            if (tokens.Length > 0 && int.TryParse(tokens[0], out var major))
            {
                if (major == 1 && tokens.Length > 1 && int.TryParse(tokens[1], out var legacy)) return legacy;
                return major;
            }
        }

        return 0;
    }

    public int GetRequiredJavaMajor(string minecraftVersion)
    {
        if (minecraftVersion.StartsWith("1.20.5") || minecraftVersion.StartsWith("1.21") || minecraftVersion.StartsWith("1.22")) return 21;
        if (minecraftVersion.StartsWith("1.18") || minecraftVersion.StartsWith("1.19") || minecraftVersion.StartsWith("1.20")) return 17;
        return 8;
    }

    private static void AddCandidate(HashSet<string> candidates, string? value, bool javaHome = true)
    {
        if (string.IsNullOrWhiteSpace(value)) return;
        var candidate = javaHome ? Path.Combine(value, "bin", OperatingSystem.IsWindows() ? "java.exe" : "java") : Path.Combine(value, OperatingSystem.IsWindows() ? "java.exe" : "java");
        if (File.Exists(candidate)) candidates.Add(candidate);
    }

    private static IEnumerable<string> GetKnownJavaRoots()
    {
        if (OperatingSystem.IsWindows())
        {
            yield return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Java");
            yield return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Eclipse Adoptium");
        }
        else
        {
            yield return "/usr/lib/jvm";
            yield return "/Library/Java/JavaVirtualMachines";
        }
    }
}
