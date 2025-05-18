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
        viewModel.Initialize();
    }

    protected override void OnNavigatedFrom(NavigationEventArgs e)
    {
        base.OnNavigatedFrom(e);
        viewModel?.OnNavigatedFrom?.Execute(null);
    }
}
