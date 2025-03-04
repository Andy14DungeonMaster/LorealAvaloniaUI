using Avalonia;
using System;
using System.IO;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Avalonia.Threading;
using LorealAvaloniaUI.ViewModels;
using LorealAvaloniaUI.Views;
using LorealAvaloniaUI.Services;

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

            // ✅ Load Configuration from appsettings.json
            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .Build();
            serviceCollection.AddSingleton<IConfiguration>(configuration);

            // ✅ Register ViewModels
            serviceCollection.AddSingleton<MainViewModel>();
            serviceCollection.AddTransient<DashboardViewModel>();
            serviceCollection.AddTransient<SettingsViewModel>();
            serviceCollection.AddTransient<DownloadViewModel>();  
            serviceCollection.AddTransient<OneDriveViewModel>();  
            serviceCollection.AddTransient<OutlookFilesViewModel>();  

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
                    mainWindow.Show();
                    mainWindow.Activate();
                });
            }

            base.OnFrameworkInitializationCompleted();
        }
    }
}