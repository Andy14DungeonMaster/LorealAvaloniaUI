using System;
using Avalonia.Controls;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using LorealAvaloniaUI.ViewModels;

namespace LorealAvaloniaUI.Views
{
    public partial class DownloadView : UserControl
    {
        public DownloadView()
        {
            InitializeComponent();

            // ✅ Ensure ServiceProvider is not null
            if (Program.ServiceProvider is null)
            {
                Console.WriteLine("Error: ServiceProvider is null");
                return;
            }

            try
            {
                var config = Program.ServiceProvider.GetService<IConfiguration>();

                if (config is null)
                {
                    Console.WriteLine("Error: IConfiguration service is missing");
                    return;
                }

                DataContext = new DownloadViewModel(config);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error initializing DownloadViewModel: {ex.Message}");
            }
        }
    }
}