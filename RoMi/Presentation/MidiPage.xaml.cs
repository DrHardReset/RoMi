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
    }
}
