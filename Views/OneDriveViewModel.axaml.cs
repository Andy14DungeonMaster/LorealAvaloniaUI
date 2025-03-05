using Avalonia.Controls;
using LorealAvaloniaUI.ViewModels;

namespace LorealAvaloniaUI.Views;

public partial class OneDriveView : UserControl
{
    public OneDriveView()
    {
        InitializeComponent();
        DataContext = new OneDriveViewModel();
    }
} 