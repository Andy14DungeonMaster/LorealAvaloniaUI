using Avalonia;
using System;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;
using Avalonia.Threading;
using LorealAvaloniaUI.ViewModels;
using LorealAvaloniaUI.Views;
using LorealAvaloniaUI.Services;

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
        serviceCollection.AddTransient<DownloadViewModel>();  // ✅ Fix missing ViewModel
        serviceCollection.AddTransient<OneDriveViewModel>();  // ✅ Fix missing ViewModel
        serviceCollection.AddTransient<OutlookFilesViewModel>();  // ✅ Fix missing ViewModel

        // ✅ Register Services
        serviceCollection.AddSingleton<NavigationService>();

        Services = serviceCollection.BuildServiceProvider();
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            Dispatcher.UIThread.Post(() =>
            {
                var mainWindow = new MainWindow
                {
                    DataContext = Services.GetRequiredService<MainViewModel>()
                };

                desktop.MainWindow = mainWindow;
                mainWindow.Show();  // ✅ Ensure the window appears
                mainWindow.Activate();  // ✅ Bring window to the front
            });
        }

        base.OnFrameworkInitializationCompleted();
    }
}