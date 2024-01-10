using System.Globalization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Localization;

namespace RoMi
{
    public class App : Application
    {
        public static Window? MainWindow { get; private set; }
        protected IHost? Host { get; private set; }

        protected async override void OnLaunched(LaunchActivatedEventArgs args)
        {
            var builder = this.CreateBuilder(args)
                // Add navigation support for toolkit controls such as TabBar and NavigationView
                .UseToolkitNavigation()
                .Configure(host => host
#if DEBUG
                    // Switch to Development environment when running in DEBUG
                    .UseEnvironment(Environments.Development)
#endif
                    .UseLogging(configure: (context, logBuilder) =>
                    {
                        // Configure log levels for different categories of logging
                        logBuilder
                            .SetMinimumLevel(
                                context.HostingEnvironment.IsDevelopment() ?
                                    LogLevel.Information :
                                    LogLevel.Warning)

                            // Default filters for core Uno Platform namespaces
                            .CoreLogLevel(LogLevel.Warning);

                        // Uno Platform namespace filter groups
                        // Uncomment individual methods to see more detailed logging
                        //// Generic Xaml events
                        //logBuilder.XamlLogLevel(LogLevel.Debug);
                        //// Layout specific messages
                        //logBuilder.XamlLayoutLogLevel(LogLevel.Debug);
                        //// Storage messages
                        //logBuilder.StorageLogLevel(LogLevel.Debug);
                        //// Binding related messages
                        //logBuilder.XamlBindingLogLevel(LogLevel.Debug);
                        //// Binder memory references tracking
                        //logBuilder.BinderMemoryReferenceLogLevel(LogLevel.Debug);
                        //// DevServer and HotReload related
                        //logBuilder.HotReloadCoreLogLevel(LogLevel.Information);
                        //// Debug JS interop
                        //logBuilder.WebAssemblyLogLevel(LogLevel.Debug);

                    }, enableUnoLogging: true)

                    .ConfigureServices((context, services) =>
                    {
                        services.Configure<RequestLocalizationOptions>(options =>
                        {
                            var supportedCultures = new[] { "en", "de" };
                            options.SetDefaultCulture(supportedCultures[0])
                                .AddSupportedCultures(supportedCultures)
                                .AddSupportedUICultures(supportedCultures);
                        });

                        services.AddHttpClient("PdfDownloadClient");
                    })

                    // Enable localization (see appsettings.json for supported languages)
                    .UseLocalization()

                    .UseNavigation(RegisterRoutes)
                );
            MainWindow = builder.Window;

#if DEBUG
            MainWindow.EnableHotReload();
#endif

            Host = await builder.NavigateAsync<Shell>();
        }

        private static void RegisterRoutes(IViewRegistry views, IRouteRegistry routes)
        {
            views.Register(
                new ViewMap(ViewModel: typeof(ShellViewModel)),
                new ViewMap<MainPage, MainViewModel>(),
                new ViewMap<AboutPage, AboutViewModel>(),
                new DataViewMap<MidiPage, MidiViewModel, MidiDocument>()
            );

            routes.Register(
                new RouteMap("", View: views.FindByViewModel<ShellViewModel>(),
                    Nested: new RouteMap[]
                    {
                       new RouteMap("Main", View: views.FindByViewModel<MainViewModel>()),
                        new RouteMap("About", View: views.FindByViewModel<AboutViewModel>()),
                        new RouteMap("Midi", View: views.FindByViewModel<MidiViewModel>())
                    }
                )
            );
        }
    }
}
