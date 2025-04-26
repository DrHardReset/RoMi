using Windows.ApplicationModel.DataTransfer;

namespace RoMi.Presentation;

public sealed partial class Dt1ContentDialog : ContentDialog
{
    private readonly string hexString;
    private readonly string valueString;

    public Dt1ContentDialog(string hexString, string valueString)
    {
        InitializeComponent();

        if (App.MainWindow != null && App.MainWindow.Content is FrameworkElement frameworkElement)
        {
            XamlRoot = frameworkElement.XamlRoot;
        }

        this.hexString = hexString;
        this.valueString = valueString;
        TextContent.Text = $"Hex:\t{hexString}\nValue:\t{valueString}";
    }

    private void CopyHex_Click(object sender, RoutedEventArgs e)
    {
        DataPackage dataPackage = new();
        dataPackage.SetText(hexString);
        Clipboard.SetContent(dataPackage);
    }

    private void CopyValue_Click(object sender, RoutedEventArgs e)
    {
        DataPackage dataPackage = new();
        dataPackage.SetText(valueString);
        Clipboard.SetContent(dataPackage);
    }

    private void Close_Click(object sender, RoutedEventArgs e)
    {
        Hide();
    }
}

