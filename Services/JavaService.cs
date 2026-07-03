using System;
using System.IO;

namespace NexusLauncher.Services;

public class JavaService
{
    public string? FindJavaPath()
    {
        var javaHome = Environment.GetEnvironmentVariable("JAVA_HOME");
        if (!string.IsNullOrWhiteSpace(javaHome))
        {
            var candidate = Path.Combine(javaHome, "bin", OperatingSystem.IsWindows() ? "java.exe" : "java");
            if (File.Exists(candidate)) return candidate;
        }

        foreach (var pathValue in Environment.GetEnvironmentVariable("Path")?.Split(Path.PathSeparator) ?? Array.Empty<string>())
        {
            if (string.IsNullOrWhiteSpace(pathValue)) continue;

            var candidate = Path.Combine(pathValue, OperatingSystem.IsWindows() ? "java.exe" : "java");
            if (File.Exists(candidate)) return candidate;
        }

        return null;
    }
}
