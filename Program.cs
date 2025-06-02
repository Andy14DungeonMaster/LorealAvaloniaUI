using System;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.ReactiveUI;
using Microsoft.Extensions.DependencyInjection;
using LorealAvaloniaUI.Services;
using LorealAvaloniaUI.ViewModels;
using LorealAvaloniaUI.Views;
using Serilog;
using System.IO;
using Avalonia.Controls;

namespace LorealAvaloniaUI;

sealed class Program
{
    public static IServiceProvider? ServiceProvider { get; private set; }
    private static string networkSharePath = @"\\usnaccmpr1a.na.loreal.intra\Loreal_DiskCleanUp_App_Logs";


    [STAThread]
    public static void Main(string[] args)
    {
        string logFileNamePattern = $"Logs\\{Environment.MachineName}_.log";

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.Console()
            .WriteTo.File(logFileNamePattern, rollingInterval: RollingInterval.Day)
            .CreateLogger();

        Log.Information("________________________________________________________________");
        Log.Information($"User logged in: {Environment.UserDomainName}\\{Environment.UserName}");
        Log.Information($"Session Initiated: {DateTime.Now}");
        Log.Information("________________________________________________________________");

        //string fileName = $"{Environment.MachineName}_.log";
        //string relativePath = Path.Combine("Logs", fileName);  // Construct relative path
        //string fullPath = Path.GetFullPath(relativePath);      // Get the full, absolute path

        //CopyLogFileToNetworkShare();


        //BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);

        //CopyLogFileToNetworkShare();

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
    {
        var builder = AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .UseReactiveUI()
            .LogToTrace();


        builder.AfterSetup(_ =>
        {
            if (Application.Current.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                desktop.Exit += OnAppClosed;
            }
        });

        return builder;
    }

    private static void OnAppClosed(object? sender, ControlledApplicationLifetimeExitEventArgs e)
    {
        CopyLogFileToNetworkShare();
    }


    private static void CopyLogFileToNetworkShare()
    {
        try
        {
            string fileName = $"{Environment.MachineName}_{DateTime.Now:yyyyMMdd}.log";
            string relativePath = Path.Combine("Logs", fileName);  // Construct relative path
            string fullPath = Path.GetFullPath(relativePath);      // Get the full, absolute path

            // Ensure the network share directory exists.  If not, create it.
            Directory.CreateDirectory(networkSharePath);


            string destinationLogPath = Path.Combine(networkSharePath, fileName);

            File.Copy(fullPath, destinationLogPath, true); // true = overwrite if exists

            Log.Information($"Log file copied to: {destinationLogPath}");
        }
        catch (Exception ex)
        {
            Log.Error($"Error copying log file: {ex.Message}");

            // Consider more robust error handling here based on what makes sense for your application.
            // For example, you could retry the copy, notify the user, or log more details about the exception.

        }
    }

}