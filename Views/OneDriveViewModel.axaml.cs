using Avalonia.Controls;
using LorealAvaloniaUI.ViewModels;
using Serilog;

namespace LorealAvaloniaUI.Views;

public partial class OneDriveView : UserControl
{
    public OneDriveView()
    {
        InitializeComponent();
        DataContext = new OneDriveViewModel();
        Log.Information("**One Drive Menu Selected**");
    }
} 