namespace RoMi.Presentation;

/// <summary>
/// The initial page is defined by the default route (IsDefault) in App.RegisterRoutes.
/// Do not navigate from this constructor: view models are created on a thread pool thread,
/// which makes the navigation continue off the UI thread and crash on WinAppSDK (RPC_E_WRONG_THREAD).
/// </summary>
public class ShellViewModel
{
}
