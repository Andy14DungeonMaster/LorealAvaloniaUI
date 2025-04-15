using System;
using Avalonia.Controls;
using LorealAvaloniaUI.ViewModels;
using ReactiveUI;
using Serilog;

namespace LorealAvaloniaUI.Views;

public partial class DownloadView : UserControl
{
    public DownloadView()
    {
        InitializeComponent();
        DataContext = new DownloadViewModel();
        Log.Information("**Downloads Menu Selected**");

    }

}
