using Avalonia.Controls;
using Microsoft.Extensions.DependencyInjection;
using LorealAvaloniaUI.Services;

namespace LorealAvaloniaUI.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        var navigationService = App.Services.GetRequiredService<NavigationService>();
        navigationService.Initialize(MainContent);
    }
}