using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace NexusLauncher.Minecraft.Java;

public sealed record JavaInstallation(string Path, int MajorVersion);

public class JavaManager
{
    public string? FindJavaPath(int minimumMajor = 17) => FindBestJava(minimumMajor)?.Path;

    public JavaInstallation? FindBestJava(int minimumMajor = 17)
        => FindInstalledJavas().Where(j => j.MajorVersion >= minimumMajor).OrderBy(j => j.MajorVersion).FirstOrDefault();

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
            catch
            {
            }
        }

        return candidates.Select(path => new JavaInstallation(path, GetMajorVersion(path))).Where(j => j.MajorVersion > 0)
            .GroupBy(j => j.Path, StringComparer.OrdinalIgnoreCase).Select(g => g.First()).OrderBy(j => j.MajorVersion).ToList();
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
        catch { return 0; }
    }

    public bool IsJavaVersionSupported(string versionOutput) => ParseMajorVersion(versionOutput) >= 17;

    public static int ParseMajorVersion(string versionOutput)
    {
        if (string.IsNullOrWhiteSpace(versionOutput)) return 0;
        var match = Regex.Match(versionOutput, "version\\s+\\\"(?<version>[^\\\"]+)\\\"");
        if (!match.Success) return 0;
        var tokens = match.Groups["version"].Value.Split('.', '_', '-', StringSplitOptions.RemoveEmptyEntries);
        if (tokens.Length == 0 || !int.TryParse(tokens[0], out var major)) return 0;
        if (major == 1 && tokens.Length > 1 && int.TryParse(tokens[1], out var legacy)) return legacy;
        return major;
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
