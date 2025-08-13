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
using Serilog;

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
        serviceCollection.AddTransient<SettingsViewModel>();
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
        // Get the current UI culture of the operating system
        CultureInfo osCulture = CultureInfo.CurrentUICulture;

        // Set the application's resource culture to the OS culture
        Lang.Resources.Culture = osCulture;

        // For demonstration, you could print it or use it for logging
        Log.Information($"Operating System Language: {osCulture.DisplayName} ({osCulture.Name})");

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