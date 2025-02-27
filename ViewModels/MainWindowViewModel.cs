using System.Collections.ObjectModel;
using ReactiveUI;

namespace LorealAvaloniaUI.ViewModels
{
   public class MainViewModel : ReactiveObject
{
    public ObservableCollection<MenuItemViewModel> MenuItems { get; set; }

    private MenuItemViewModel? _selectedMenuItem;
    public MenuItemViewModel? SelectedMenuItem
    {
        get => _selectedMenuItem;
        set => this.RaiseAndSetIfChanged(ref _selectedMenuItem, value);
    }

    public MainViewModel()
    {
        MenuItems = new ObservableCollection<MenuItemViewModel>
        {
            new MenuItemViewModel
            {
                Title = "Overview",
                // Children = new ObservableCollection<MenuItemViewModel>
                // {
                //     new MenuItemViewModel { Title = "Dashboard" },
                //     new MenuItemViewModel { Title = "Reports" }
                // }
            },
            new MenuItemViewModel
            {
                Title = "Disk Storage",
                Children = new ObservableCollection<MenuItemViewModel>
                {
                    new MenuItemViewModel { Title = "Download" },
                    new MenuItemViewModel { Title = "One Drive" },
                    new MenuItemViewModel { Title = "Outlook Files" }
                }
            }
        };
    }
}
}
