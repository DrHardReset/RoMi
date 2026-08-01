using Microsoft.UI.Xaml.Data;

namespace RoMi.Presentation.Converters;

public partial class StringToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        bool isEmpty = value is not string stringValue || string.IsNullOrEmpty(stringValue);
        bool invert = string.Equals(parameter as string, "Invert", StringComparison.OrdinalIgnoreCase);

        bool isVisible = invert ? isEmpty : !isEmpty;

        return isVisible ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}
