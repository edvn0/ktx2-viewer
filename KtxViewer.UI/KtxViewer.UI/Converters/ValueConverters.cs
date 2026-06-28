using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace KtxViewer.UI.Converters;

public sealed class NullToVisibilityConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var isInverse = parameter?.ToString() == "Inverse";
        var isNull = value == null;

        if (targetType == typeof(bool))
        {
            return isInverse ? !isNull : isNull;
        }

        return (isInverse ? !isNull : isNull) ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public sealed class BoolToVisibilityConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is bool b && b ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public sealed class FileNameConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is string path ? System.IO.Path.GetFileName(path) : value ?? string.Empty;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Returns parameter / value. Used to keep an overlay stroke at a constant device
/// thickness while it sits inside a scaled (zoomed) container: thickness = base / zoom.
/// </summary>
public sealed class InverseScaleConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var baseThickness = 1.5;
        if (parameter != null)
        {
            double.TryParse(parameter.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out baseThickness);
        }

        if (value is double scale && scale > 0.0001)
        {
            return baseThickness / scale;
        }

        return baseThickness;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public sealed class IndexToNumberConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is int index && index >= 0)
        {
            return $"{index + 1}.";
        }

        return string.Empty;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public sealed class SubtractConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is double d)
        {
            var amount = 0.0;
            if (parameter != null)
            {
                double.TryParse(parameter.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out amount);
            }

            return Math.Max(0, d - amount);
        }

        return value ?? 0.0;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public sealed class BoolToThicknessConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool b && b)
        {
            var thickness = parameter?.ToString() ?? "2";
            return new Thickness(double.Parse(thickness));
        }
        return new Thickness(0);
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
