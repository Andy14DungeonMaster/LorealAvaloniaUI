using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using LorealAvaloniaUI.Services;
using LorealAvaloniaUI.ViewModels;
using LorealAvaloniaUI.Views;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using System;
using System.IO;

namespace LorealAvaloniaUI
{
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

            // Configure Serilog
            var logFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "LorealAvaloniaUI", "deletion_log.txt");
            Directory.CreateDirectory(Path.GetDirectoryName(logFilePath));

            Log.Logger = new LoggerConfiguration()
                .WriteTo.Console()
                .WriteTo.File(logFilePath, rollingInterval: RollingInterval.Day, retainedFileCountLimit: 7)
                .CreateLogger();

            // Register ViewModels
            serviceCollection.AddSingleton<MainViewModel>();
            serviceCollection.AddTransient<DashboardViewModel>();
            serviceCollection.AddTransient<SettingsViewModel>();
            serviceCollection.AddTransient<DownloadViewModel>();
            serviceCollection.AddTransient<OneDriveViewModel>();
            serviceCollection.AddTransient<OutlookFilesViewModel>();
            serviceCollection.AddTransient<OfficeFileCacheViewModel>();

            // Register Services
            serviceCollection.AddSingleton<NavigationService>();

            Services = serviceCollection.BuildServiceProvider();
        }

        public override void OnFrameworkInitializationCompleted()
        {
            // Update LastUsedDate on app launch
            FileDeletionTracker.Instance.LastUsedDate = DateTime.UtcNow;

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
}