using System;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;

namespace NexusLauncher.Minecraft.Java;

public class JavaManager
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

    public bool IsJava17OrHigher(string? javaPath)
    {
        if (string.IsNullOrWhiteSpace(javaPath)) return false;

        try
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = javaPath,
                    Arguments = "-version",
                    RedirectStandardError = true,
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            process.Start();
            var output = process.StandardError.ReadToEnd() + process.StandardOutput.ReadToEnd();
            process.WaitForExit(5000);

            return IsJavaVersionSupported(output);
        }
        catch
        {
            return false;
        }
    }

    public bool IsJavaVersionSupported(string versionOutput)
    {
        if (string.IsNullOrWhiteSpace(versionOutput)) return false;

        var match = Regex.Match(versionOutput, "version\\s+\\\"(?<version>[^\\\"]+)\\\"");
        if (!match.Success) return false;

        var versionText = match.Groups["version"].Value;
        var tokens = versionText.Split('.', StringSplitOptions.RemoveEmptyEntries);
        if (tokens.Length == 0) return false;

        if (int.TryParse(tokens[0], out var major))
        {
            if (tokens[0] == "1")
            {
                return false;
            }

            return major >= 17;
        }

        return false;
    }
}
