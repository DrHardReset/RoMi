namespace RoMi.Presentation
{
    public sealed partial class MainPage : Page
    {
        public MainPage()
        {
            InitializeComponent();

            // Loaded event does not fire on Android -> use DataContextChanged
            DataContextChanged += MainPage_Loaded;
        }

        private void MainPage_Loaded(FrameworkElement sender, DataContextChangedEventArgs e)
        {
            if (e.NewValue is not MainViewModel)
            {
                return;
            }

            MainViewModel mainViewModel = (e.NewValue as MainViewModel)!;
            mainViewModel.OnPageLoaded.Execute(null);
            DataContextChanged -= MainPage_Loaded;
        }

        private void BtnShowHide_Click(object sender, RoutedEventArgs e)
        {
            if (StackPanelPdfInput.Visibility == Visibility.Visible)
            {
                StackPanelPdfInput.Visibility = Visibility.Collapsed;
                BtnShowHide.Content = "+";
            }
            else
            {
                StackPanelPdfInput.Visibility = Visibility.Visible;
                BtnShowHide.Content = "-";
            }
        }
    }
}
