using Microsoft.UI.Xaml.Data;
using RoMi.Models.Converters;

namespace RoMi.Presentation.Converters;

// class shall be partial for trimming and AOT compatibility
public partial class ByteArrayToStringConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        return ((byte[])value).ByteArrayToHexString();
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        return ""; // not used at the moment
    }
}
