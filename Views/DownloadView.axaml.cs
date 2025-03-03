using Avalonia.Controls;
using LorealAvaloniaUI.ViewModels;

namespace LorealAvaloniaUI.Views;

public partial class DownloadView : UserControl
{
    public DownloadView()
    {
        InitializeComponent();
          DataContext = new DownloadViewModel(); 
    }
}