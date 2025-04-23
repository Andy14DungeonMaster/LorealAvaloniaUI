using Avalonia.Controls;
using Microsoft.Extensions.DependencyInjection;
using Avalonia.Input;
using LorealAvaloniaUI.Services;
using LorealAvaloniaUI.ViewModels;
using System;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Platform;
using Avalonia;
using Avalonia.LogicalTree;

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
        if (sender is Control control && control.DataContext is MenuItemViewModel menuItem)
        {
            menuItem.Command?.Execute().Subscribe();
        }
    }


    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        PositionWindow();
    }

    private void PositionWindow()
    {
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var screen = Screens.Primary;
            double scaling = screen.Scaling;  

            // Convert the raw working area (in pixels) to logical (DIP) units.
            double logicalWorkingWidth = screen.WorkingArea.Width / scaling;
            double logicalWorkingHeight = screen.WorkingArea.Height / scaling;

            
            double desiredWidthLogical = logicalWorkingWidth * 0.7;
            double desiredHeightLogical = logicalWorkingHeight * 0.7;


            // Set the window size in DIPs.
            this.Width = desiredWidthLogical;
            this.Height = desiredHeightLogical;

           
            int posX = screen.WorkingArea.X +
                       (screen.WorkingArea.Width - (int)(desiredWidthLogical * scaling)) / 2;
            int posY = screen.WorkingArea.Y +
                       (screen.WorkingArea.Height - (int)(desiredHeightLogical * scaling)) / 2;

            this.Position = new PixelPoint(posX, posY);
        }
    }
   
}