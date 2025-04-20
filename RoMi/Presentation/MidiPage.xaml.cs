using Windows.ApplicationModel.DataTransfer;

namespace RoMi.Presentation;

public sealed partial class MidiPage : Page
{
    MidiViewModel? viewModel;

    public MidiPage()
    {
        InitializeComponent();
        DataContextChanged += MainPage_Loaded;
    }

    private void MainPage_Loaded(FrameworkElement sender, DataContextChangedEventArgs e)
    {
        if (e.NewValue is not MidiViewModel)
        {
            return;
        }

        // Store the viewModel for later use in OnNavigatedFrom
        viewModel = (e.NewValue as MidiViewModel)!;
    }

    protected override void OnNavigatedFrom(NavigationEventArgs e)
    {
        base.OnNavigatedFrom(e);
        viewModel?.OnNavigatedFrom.Execute(null);
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
        ComboBox? comboBox = sender as ComboBox;

        if (comboBox == null)
        {
            return;
        }

        string? selectedText = comboBox.SelectedItem?.ToString();
        Visibility visibility = string.IsNullOrWhiteSpace(selectedText) ? Visibility.Collapsed : Visibility.Visible;
        comboBox.Visibility = visibility;
    }
}
