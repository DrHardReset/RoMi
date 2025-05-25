using Windows.ApplicationModel.DataTransfer;

namespace RoMi.Presentation.Controls;

public sealed partial class MidiIoComboboxControl : UserControl
{
    private readonly MidiIoComboboxControlViewModel? viewModel;

    public MidiIoComboboxControl(MidiIoComboboxControlViewModel viewModel)
    {
        this.viewModel = viewModel;
        InitializeComponent();
        DataContext = this.viewModel;
    }

    // Fallback constructor for Designer and XAML-Preview
    public MidiIoComboboxControl()
    {
        InitializeComponent();
    }

    private void CopyDt1ToClipboard_Click(object sender, RoutedEventArgs e)
    {
        DataPackage dataPackage = new();
        dataPackage.SetText(CalculatedSysexDt1.Text);
        Clipboard.SetContent(dataPackage);
    }

    private void CopyRq1ToClipboard_Click(object sender, RoutedEventArgs e)
    {
        DataPackage dataPackage = new();
        dataPackage.SetText(CalculatedSysexRq1.Text);
        Clipboard.SetContent(dataPackage);
    }

    private void ComboBoxBranchTable_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (sender is not ComboBox comboBox)
        {
            return;
        }

        string? selectedText = comboBox.SelectedItem?.ToString();
        Visibility visibility = string.IsNullOrWhiteSpace(selectedText) ? Visibility.Collapsed : Visibility.Visible;
        comboBox.Visibility = visibility;
    }
}
