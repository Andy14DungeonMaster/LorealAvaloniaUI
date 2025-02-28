using System;
using System.Collections.ObjectModel;
using System.Reactive;
using Avalonia.Threading;
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
            set
            {
                this.RaiseAndSetIfChanged(ref _selectedMenuItem, value);
                if (value?.Command != null)
                {
                    value.Command.Execute().Subscribe();
                }
            }
        }

        public MainViewModel(NavigationService navigationService)
        {
            _navigationService = navigationService;

            var navigateToDownload = ReactiveCommand.Create(() =>
            {
                Dispatcher.UIThread.Post(() => _navigationService.Navigate<DownloadViewModel, DownloadView>());
            });

            MenuItems = new ObservableCollection<MenuItemViewModel>
            {
                new MenuItemViewModel("Overview", ReactiveCommand.Create(() =>
                {
                    Dispatcher.UIThread.Post(() => _navigationService.Navigate<DashboardViewModel, DashboardView>());
                })),

                new MenuItemViewModel("Disk Storage")
                {
                    Children =
                    {
                        new MenuItemViewModel("Download", navigateToDownload),  // ✅ Ensure this executes correctly
                        new MenuItemViewModel("One Drive", ReactiveCommand.Create(() =>
                        {
                            Dispatcher.UIThread.Post(() => _navigationService.Navigate<OneDriveViewModel, OneDriveView>());
                        })),
                        new MenuItemViewModel("Outlook Files", ReactiveCommand.Create(() =>
                        {
                            Dispatcher.UIThread.Post(() => _navigationService.Navigate<OutlookFilesViewModel, OutlookFilesView>());
                        }))
                    }
                }
            };
        }
    }
}