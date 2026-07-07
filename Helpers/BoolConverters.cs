using Avalonia.Data.Converters;
using System;
using System.Globalization;

namespace NexusLauncher.Helpers;

public static class BoolConverters
{
    public static readonly IValueConverter Not = new BoolInvertConverter();
}

public static class ObjectConverters
{
    public static readonly IValueConverter IsNull = new ObjectIsNullConverter();
    public static readonly IValueConverter IsNotNull = new ObjectIsNotNullConverter();
}

public class BoolInvertConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is bool b ? !b : value;

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is bool b ? !b : value;
}

public class ObjectIsNullConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is null;

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

public class ObjectIsNotNullConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is not null;

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
