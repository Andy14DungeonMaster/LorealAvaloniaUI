using System;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.ReactiveUI;
using Microsoft.Extensions.DependencyInjection;
using LorealAvaloniaUI.Services;
using LorealAvaloniaUI.ViewModels;
using LorealAvaloniaUI.Views;

namespace LorealAvaloniaUI;

sealed class Program
{
    public static IServiceProvider? ServiceProvider { get; private set; }

    [STAThread]
    public static void Main(string[] args)
    {
        var services = new ServiceCollection();

        // ✅ Register ViewModels
        services.AddSingleton<MainViewModel>();
        services.AddTransient<DashboardViewModel>();
        services.AddTransient<SettingsViewModel>();
        services.AddTransient<DownloadViewModel>();  // ✅ Register ViewModel

        // ✅ Register Views
        services.AddTransient<MainWindow>();
        services.AddTransient<DashboardView>();
        services.AddTransient<SettingsView>();
        services.AddTransient<DownloadView>();

        // ✅ Register Services
        services.AddSingleton<NavigationService>();

        ServiceProvider = services.BuildServiceProvider();

        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
    }

    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .UseReactiveUI()
            .LogToTrace();
}