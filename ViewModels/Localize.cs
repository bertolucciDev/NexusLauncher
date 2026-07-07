namespace NexusLauncher.ViewModels;

public static class Localize
{
    public static string RamAllocated(int allocated, int max) => $"RAM: {allocated} GB / {max} GB";
    public const string Saved = "✓ Configurações salvas";
    public const string Loaded = "Pronto";
    public const string Restored = "↺ Padrões restaurados";
    public const string JavaDetected = "✓ Java detectado";
    public const string JavaNotDetected = "✕ Java não encontrado";
}
