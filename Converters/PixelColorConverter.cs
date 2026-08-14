using System;
using System.Globalization;
using Avalonia;
using Avalonia.Data.Converters;
using Avalonia.Media;
using PixelEditor.Core.Documents;

namespace pixel_editor.Converters;

public sealed class PixelColorConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not PixelColor color)
        {
            return AvaloniaProperty.UnsetValue;
        }

        return Color.FromArgb(color.Alpha, color.Red, color.Green, color.Blue);
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not Color color)
        {
            return AvaloniaProperty.UnsetValue;
        }

        return new PixelColor(color.R, color.G, color.B, color.A);
    }
}
