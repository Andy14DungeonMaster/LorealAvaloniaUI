using Avalonia.Controls;
using LorealAvaloniaUI.ViewModels;
using Serilog;

namespace LorealAvaloniaUI.Views;

public partial class OutlookFilesView : UserControl
{
    public OutlookFilesView()
    {
        InitializeComponent();
        Log.Information("------------------ Initializing Outlook page ------------------");
    }
}
