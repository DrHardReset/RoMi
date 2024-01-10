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
            CalculatedSysex.SelectAll();
            CalculatedSysex.CopySelectionToClipboard();
        }
    }
}
