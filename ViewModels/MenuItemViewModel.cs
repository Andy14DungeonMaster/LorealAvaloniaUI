using System.Collections.ObjectModel;

namespace LorealAvaloniaUI.ViewModels
{
    public class MenuItemViewModel
    {
        public string Title { get; set; }
        public ObservableCollection<MenuItemViewModel> Children { get; set; } = new();
        public bool IsExpanded { get; set; }
    }
}
