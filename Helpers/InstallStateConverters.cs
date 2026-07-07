using Avalonia.Data.Converters;
using NexusLauncher.Models;
using System;
using System.Globalization;

namespace NexusLauncher.Helpers;

public static class InstallStateConverters
{
    public static readonly IValueConverter IsIdle = new InstallStateToBoolConverter(InstallState.Idle);
    public static readonly IValueConverter IsInstalling = new InstallStateToBoolConverter(InstallState.Installing);
    public static readonly IValueConverter IsInstalled = new InstallStateToBoolConverter(InstallState.Installed);
    public static readonly IValueConverter IsError = new InstallStateToBoolConverter(InstallState.Error);
}

public class InstallStateToBoolConverter : IValueConverter
{
    private readonly InstallState _target;
    public InstallStateToBoolConverter(InstallState target) => _target = target;

    public object? Convert(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture)
        => value is InstallState state && state == _target;

    public object? ConvertBack(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture)
        => throw new System.NotSupportedException();
}