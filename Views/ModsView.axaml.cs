using Avalonia.Controls;

namespace NexusLauncher.Views;

public partial class ModsView : UserControl
{
    private const double ScrollThreshold = 300;

    public ModsView()
    {
        InitializeComponent();
    }

    private void OnScrollChanged(object? sender, ScrollChangedEventArgs e)
    {
        if (DataContext is not ViewModels.ModsViewModel vm) return;
        if (!vm.HasMoreResults || vm.IsLoadingMore) return;

        if (sender is not ScrollViewer sv) return;

        double remaining = sv.Extent.Height - sv.Offset.Y - sv.Viewport.Height;

        if (remaining <= ScrollThreshold)
        {
            if (vm.LoadMoreCommand?.CanExecute(null) == true)
                vm.LoadMoreCommand.Execute(null);
        }
    }
}
