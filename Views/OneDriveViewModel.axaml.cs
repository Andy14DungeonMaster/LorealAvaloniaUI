using Avalonia.Controls;
using Avalonia.Metadata;
using LorealAvaloniaUI.ViewModels;
using Serilog;

namespace LorealAvaloniaUI.Views;

public partial class OneDriveView : UserControl
{
    public OneDriveView()
    {
        
        InitializeComponent();
        DataContext = new OneDriveViewModel();
        Log.Information("-- Initiating OneDrive Menu --");
    }
} 