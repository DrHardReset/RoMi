using Windows.ApplicationModel.DataTransfer;

namespace RoMi.Presentation
{
    public sealed partial class MidiPage : Page
    {
        public MidiPage()
        {
            InitializeComponent();
        }

        private void CopyToClipboard_Click(object sender, RoutedEventArgs e)
        {
            DataPackage dataPackage = new();
            dataPackage.SetText(CalculatedSysex.Text);
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
}
