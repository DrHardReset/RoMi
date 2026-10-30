using Microsoft.UI.Xaml.Data;

namespace RoMi.Presentation.Converters;

public partial class BoolToTreeViewSelectionModeConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
        => value is true ? TreeViewSelectionMode.Multiple : TreeViewSelectionMode.None;

    public object ConvertBack(object value, Type targetType, object parameter, string language)
        => throw new NotSupportedException();
}
