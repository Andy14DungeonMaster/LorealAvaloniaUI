using Avalonia.Controls;
using LorealAvaloniaUI.ViewModels;
using Serilog;

namespace LorealAvaloniaUI.Views;


public partial class OfficeFileCacheView : UserControl
{
    public OfficeFileCacheView()
    {
        InitializeComponent();
        DataContext = new OfficeFileCacheViewModel();
        Log.Information("-- Initializing Office Cache Files Page --");
    }
}