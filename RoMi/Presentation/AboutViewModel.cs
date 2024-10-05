using System.Collections.ObjectModel;
using System.Reflection;

namespace RoMi.Presentation;

public partial class AboutViewModel : ObservableObject
{
    public ObservableCollection<TopLevelPackage> Packages { get; set; }

    public static string Title
    {
        get
        {
            Assembly assembly = Assembly.GetExecutingAssembly();
            AssemblyName assemlbyName = assembly.GetName();
            string version = "";

            if (assemlbyName.Version != null)
            {
                version = " " + assemlbyName.Version.ToString();
            }

            return "RoMi" + version;
        }
    }

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
            Packages = [new TopLevelPackage() { Id = "Package references could not be loaded." }];
            return;
        }

        Packages = [];

        if (dependencyReport != null)
        {
            Packages.AddRange(dependencyReport.GetAllDistinctTopLevelPackages());
        }
    }
}
