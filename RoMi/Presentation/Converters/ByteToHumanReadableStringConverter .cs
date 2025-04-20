using Microsoft.UI.Xaml.Data;

namespace RoMi.Presentation.Converters;

// class shall be partial for trimming and AOT compatibility
internal partial class ByteToHumanReadableStringConverter : IValueConverter
{
    public object? Convert(object value, Type targetType, object parameter, string language)
    {
        // ComboBox automatically casts byte to int
        if (value is int i && i >= 0 && i <= 255)
        {
            return $"0x{i:X2} ({i + 1})";
        }

        return value?.ToString();
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        if (value is string s && s.StartsWith("0x") && byte.TryParse(s.AsSpan(2, 2), System.Globalization.NumberStyles.HexNumber, null, out byte result))
        {
            return result;
        }

        return 0;
    }
}
