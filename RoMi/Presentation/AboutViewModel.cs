using System.Collections.ObjectModel;

namespace RoMi.Presentation;

public partial class AboutViewModel : ObservableObject
{
    public ObservableCollection<TopLevelPackage> Packages { get; set; }

    public AboutViewModel(INavigator navigator)
    {
        DependencyReport? dependencyReport;

        try
        {
            dependencyReport = DependencyReport.Read().Result;
        }
        catch (Exception ex)
        {
            _ = navigator.ShowMessageDialogAsync(this, title: "Error parsing dependency report file", content: $"{ex}");
            Packages = new ObservableCollection<TopLevelPackage>();
            Packages.Add(new TopLevelPackage() { Id = "Package references could not be loaded." });
            return;
        }

        Packages = new ObservableCollection<TopLevelPackage>();

        if (dependencyReport != null)
        {
            Packages.AddRange(dependencyReport.GetAllDistinctTopLevelPackages());
        }
    }
}
