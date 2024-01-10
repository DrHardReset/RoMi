using Microsoft.UI.Xaml.Data;
using RoMi.Business.Converters;

namespace RoMi.Presentation.Converters
{
    public class ByteArrayToStringConverter : IValueConverter
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
}
