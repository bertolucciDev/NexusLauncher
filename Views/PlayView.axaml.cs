using Avalonia.Controls;
using NexusLauncher.Models;

namespace NexusLauncher.Views;

public partial class PlayView : UserControl
{
    private MinecraftVersionInfo? _lastValidSelection;

    public PlayView()
    {
        InitializeComponent();
        VersionCombo.SelectionChanged += OnSelectionChanged;
    }

    private void OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (VersionCombo.SelectedItem is MinecraftVersionInfo v && v.IsGroupHeader)
        {
            VersionCombo.SelectedItem = _lastValidSelection;
        }
        else
        {
            _lastValidSelection = VersionCombo.SelectedItem as MinecraftVersionInfo;
        }
    }
}
