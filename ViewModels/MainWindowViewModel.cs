using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Reactive;
using System.Reactive.Linq;
using LorealAvaloniaUI.Lang;
using LorealAvaloniaUI.Services;
using LorealAvaloniaUI.Views;
using ReactiveUI;
using Serilog;

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

        private ObservableCollection<CultureInfo> _availableLanguages;
        public ObservableCollection<CultureInfo> AvailableLanguages
        {
            get => _availableLanguages;
            private set => this.RaiseAndSetIfChanged(ref _availableLanguages, value);
        }

        private CultureInfo? _selectedLanguage; 
        public CultureInfo? SelectedLanguage
        {
            get => _selectedLanguage;
            set
            {
                this.RaiseAndSetIfChanged(ref _selectedLanguage, value);
          
                if (value != null)
                {
                    Log.Information($"Language changed to: {value}");
                    Lang.Resources.Culture = value;

                    var x = _navigationService.CurrentViewModelType;

                    if (x != null ) 
                    {
                        if (x.Name == "DownloadViewModel")
                        {
                            _navigationService.Navigate<DownloadViewModel, DownloadView>();
                        }

                        else if (x.Name == "OneDriveViewModel")
                        {
                            _navigationService.Navigate<OneDriveViewModel, OneDriveView>();
                        }

                        else if (x.Name == "OutlookFilesViewModel")
                        {
                            _navigationService.Navigate<OutlookFilesViewModel, OutlookFilesView>();
                        }

                        else if (x.Name == "OfficeFileCacheViewModel")
                        {
                            _navigationService.Navigate<OfficeFileCacheViewModel, OfficeFileCacheView>();
                        }
                        else
                        {
                            _navigationService.Navigate<DashboardViewModel, DashboardView>();
                        }
                    }
                    else
                    {
                        Console.WriteLine("CurrentViewModelType is null. Navigation likely not completed yet.");
                    }

                   // _navigationService.Navigate<DashboardViewModel, DashboardView>();
                }
            }
        } 

        public MainViewModel(NavigationService navigationService)
        {
            _navigationService = navigationService;

            var dashboardMenuItem = new MenuItemViewModel("Overview", NavigateCommand<DashboardViewModel, DashboardView>());

            AvailableLanguages = new ObservableCollection<CultureInfo>
            {
                new CultureInfo("en-US"),
                new CultureInfo("fr-FR"), 
                new CultureInfo("pt-BR"), 
                new CultureInfo("fr-CA"), 
                new CultureInfo("es-MX"), 
            };

            SelectedLanguage = CultureInfo.CurrentUICulture;

            MenuItems = new ObservableCollection<MenuItemViewModel>
            {
                dashboardMenuItem,

                new MenuItemViewModel("Disk Storage", NavigateCommand<DashboardViewModel, DashboardView>())
                {
                    Children =
                    {
                        new MenuItemViewModel(Resources.DownloadsTitle, NavigateCommand<DownloadViewModel, DownloadView>()),
                        new MenuItemViewModel(Resources.OfficeFilesCacheTitle, NavigateCommand<OfficeFileCacheViewModel, OfficeFileCacheView>()),
                        new MenuItemViewModel("OneDrive", NavigateCommand<OneDriveViewModel, OneDriveView>()),
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