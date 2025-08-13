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
        Lang.Resources.Culture = new CultureInfo("es-MX");
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