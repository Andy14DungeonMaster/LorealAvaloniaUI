using System.Collections.ObjectModel;
using ReactiveUI;
using System.Reactive;

namespace LorealAvaloniaUI.ViewModels
{
    public class MenuItemViewModel : ReactiveObject
    {
        public string Title { get; set; }
        public ObservableCollection<MenuItemViewModel> Children { get; } = new();  // ✅ Ensure collection is initialized
        public ReactiveCommand<Unit, Unit>? Command { get; set; }  // ✅ Command should be nullable

        public MenuItemViewModel(string title, ReactiveCommand<Unit, Unit>? command = null)
        {
            Title = title;
            Command = command;
        }
    }
}