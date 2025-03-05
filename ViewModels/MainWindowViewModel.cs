using System;
using System.Collections.ObjectModel;
using System.Reactive;
using System.Reactive.Linq;
using LorealAvaloniaUI.Services;
using LorealAvaloniaUI.Views;
using ReactiveUI;

namespace LorealAvaloniaUI.ViewModels
{
    public class MainViewModel : ReactiveObject
    {
        private readonly NavigationService _navigationService;

        public ObservableCollection<MenuItemViewModel> MenuItems { get; }

        private MenuItemViewModel? _selectedMenuItem;
        public MenuItemViewModel? SelectedMenuItem
        {
            get => _selectedMenuItem;
            set => this.RaiseAndSetIfChanged(ref _selectedMenuItem, value);
        }

        public MainViewModel(NavigationService navigationService)
        {
            _navigationService = navigationService;

            var dashboardMenuItem = new MenuItemViewModel("Overview", NavigateCommand<DashboardViewModel, DashboardView>());

            MenuItems = new ObservableCollection<MenuItemViewModel>
            {
                dashboardMenuItem,

                new MenuItemViewModel("Disk Storage", NavigateCommand<DashboardViewModel, DashboardView>())
                {
                    Children =
                    {
                        new MenuItemViewModel("Download", NavigateCommand<DownloadViewModel, DownloadView>()),
                        new MenuItemViewModel("One Drive", NavigateCommand<OneDriveViewModel, OneDriveView>()),
                        new MenuItemViewModel("Outlook Files", NavigateCommand<OutlookFilesViewModel, OutlookFilesView>())
                    }
                }
            };
            SelectedMenuItem = dashboardMenuItem;
            SelectedMenuItem?.Command?.Execute().Subscribe();
        }

        private ReactiveCommand<Unit, Unit> NavigateCommand<TViewModel, TView>()
            where TViewModel : ReactiveObject
            where TView : Avalonia.Controls.Control, new()
        {
            return ReactiveCommand.Create(() => _navigationService.Navigate<TViewModel, TView>());
        }
    }
}