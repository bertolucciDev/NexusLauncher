using Avalonia.Controls;
using Avalonia.Controls.Templates;
using NexusLauncher.ViewModels.Base;
using System;

namespace NexusLauncher;

public class ViewLocator : IDataTemplate
{
    public Control? Build(object? param)
    {
        if (param is null)
            return null;

        var viewModelName = param.GetType().FullName;
        if (string.IsNullOrWhiteSpace(viewModelName))
            return null;

        var viewName = viewModelName
            .Replace(".ViewModels.", ".Views.")
            .Replace("ViewModel", "View");

        var assemblyName = typeof(App).Assembly.GetName().Name;
        var fullName = $"{viewName}, {assemblyName}";

        var type = Type.GetType(fullName);

        return type is not null
            ? (Control)Activator.CreateInstance(type)!
            : new TextBlock { Text = $"View not found: {viewName}" };
    }

    public bool Match(object? data)
        => data is ViewModelBase;
}