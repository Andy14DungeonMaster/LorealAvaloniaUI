using Avalonia.Controls;
using Microsoft.Extensions.DependencyInjection;
using Avalonia.Input;
using LorealAvaloniaUI.Services;
using LorealAvaloniaUI.ViewModels;
using System;

namespace LorealAvaloniaUI.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        var navigationService = App.Services.GetRequiredService<NavigationService>();
        navigationService.Initialize(MainContent);
    }
    private void OnMenuItemClicked(object? sender, PointerPressedEventArgs e)
        {
            if (sender is TextBlock textBlock && textBlock.DataContext is MenuItemViewModel menuItem)
            {
                menuItem.Command?.Execute().Subscribe();
            }
        }
}