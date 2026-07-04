using NexusLauncher.Minecraft.Java;
using System.Collections.Generic;

namespace NexusLauncher.Services;

public class JavaService
{
    private readonly JavaManager _javaManager = new();
    public string? FindJavaPath(int minimumMajor = 17) => _javaManager.FindJavaPath(minimumMajor);
    public IReadOnlyList<JavaInstallation> FindInstalledJavas() => _javaManager.FindInstalledJavas();
}
