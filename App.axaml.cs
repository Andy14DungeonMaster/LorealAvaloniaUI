using Avalonia;
using System;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;
using Avalonia.Threading;
using LorealAvaloniaUI.ViewModels;
using LorealAvaloniaUI.Views;
using LorealAvaloniaUI.Services;
using System.Globalization; 
using System.Threading;   

namespace LorealAvaloniaUI;

public partial class App : Application
{
    public static IServiceProvider Services { get; private set; } = null!;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
        ConfigureServices();
    }

    private void ConfigureServices()
    {
        var serviceCollection = new ServiceCollection();

        // ✅ Register ViewModels
        serviceCollection.AddSingleton<MainViewModel>();
        serviceCollection.AddTransient<DashboardViewModel>();
        serviceCollection.AddTransient<DownloadViewModel>();
        serviceCollection.AddTransient<OneDriveViewModel>();
        serviceCollection.AddTransient<OutlookFilesViewModel>();
        serviceCollection.AddTransient<OfficeFileCacheViewModel>();

        // ✅ Register Services
        serviceCollection.AddSingleton<NavigationService>();
        serviceCollection.AddSingleton<DownloadInfoService>();
        serviceCollection.AddSingleton<OfficeCacheInfoService>();
        serviceCollection.AddSingleton<FileDeletionTracker>();

        Services = serviceCollection.BuildServiceProvider();
    }

    public override void OnFrameworkInitializationCompleted()
    {
        // --- START Language Detection and Fallback Logic ---
        // 1. Get the user's preferred UI culture from the operating system.
        CultureInfo systemCulture = CultureInfo.CurrentUICulture;

        // 2. Define a list of supported cultures in your application.
        //    Ensure these match your .resx file suffixes (e.g., Resources.fr-CA.resx)
        //    Include "en" or "en-US" as your primary fallback if your default Resources.resx is English.
        string[] supportedCultures = { "en-US", "fr-CA", "es-MX", "fr", "es", "en" };

        CultureInfo selectedCulture = CultureInfo.InvariantCulture; // Initialize with InvariantCulture for fallback

        // 3. Iterate through supported cultures to find the best match for the system's UI culture.
        foreach (string supportedCultureName in supportedCultures)
        {
            try
            {
                // Create a CultureInfo object from the supported culture name
                CultureInfo currentSupportedCulture = new CultureInfo(supportedCultureName);

                // Check if the system's UI culture matches the supported culture directly (e.g., fr-CA == fr-CA)
                // Or if the system's UI culture's parent matches the supported culture (e.g., fr-BE matches fr)
                if (systemCulture.Name.Equals(currentSupportedCulture.Name, StringComparison.OrdinalIgnoreCase) ||
                    (systemCulture.Parent != null && systemCulture.Parent.Name.Equals(currentSupportedCulture.Name, StringComparison.OrdinalIgnoreCase)))
                {
                    selectedCulture = currentSupportedCulture;
                    break; // Found a match, use this culture and stop searching
                }
            }
            catch (CultureNotFoundException)
            {
                // This catch block handles cases where a string in supportedCultures might not be a valid culture name.
                // For well-known cultures like "en-US", "fr-CA", this is unlikely but good practice.
                Console.WriteLine($"Warning: Culture '{supportedCultureName}' not found or invalid.");
            }
        }

        // If no match was found (selectedCulture is still InvariantCulture),
        // the ResourceManager will automatically fall back to the default resources (your English .resx).

        // 4. Set the UI culture for the current thread.
        //    This tells the ResourceManager which localized resources to load.
        Thread.CurrentThread.CurrentUICulture = selectedCulture;

        // For modern .NET applications, also set the default thread UI culture.
        // This ensures consistent behavior for tasks started after this point.
        CultureInfo.DefaultThreadCurrentUICulture = selectedCulture;
        // --- END Language Detection and Fallback Logic ---


        // The rest of your existing initialization code
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            Dispatcher.UIThread.Post(() =>
            {
                var mainWindow = new MainWindow
                {
                    DataContext = Services.GetRequiredService<MainViewModel>()
                };

                desktop.MainWindow = mainWindow;
                mainWindow.Show();
                mainWindow.Activate();
            });
        }

        base.OnFrameworkInitializationCompleted();
    }
}