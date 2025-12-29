namespace RoMi.Presentation;

public class ShellViewModel
{
    private readonly INavigator navigator;

    public ShellViewModel(INavigator navigator)
    {
        this.navigator = navigator;
        _ = Start();
    }

    public async Task Start()
    {
        await navigator.NavigateViewModelAsync<MainViewModel>(this);
    }
}
