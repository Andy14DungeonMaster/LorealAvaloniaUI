// In LorealAvaloniaUI.ViewModels/MainViewModel.cs
using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading;

using LorealAvaloniaUI.Lang;
using LorealAvaloniaUI.Services;
using LorealAvaloniaUI.Views;
using LorealAvaloniaUI.Messages; // Add this for your message
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

        // Language Switching Properties (controlled by MainViewModel)
        public ObservableCollection<CultureInfo> AvailableLanguages { get; }

        private CultureInfo? _selectedLanguage;
        public CultureInfo? SelectedLanguage
        {
            get => _selectedLanguage;
            set => this.RaiseAndSetIfChanged(ref _selectedLanguage, value);
        }

        public MainViewModel(NavigationService navigationService)
        {
            _navigationService = navigationService;

            // Initialize available languages
            AvailableLanguages = new ObservableCollection<CultureInfo>
            {
                new CultureInfo("en-US"), // English (United States)
                new CultureInfo("fr-CA"), // French (Canada)
                new CultureInfo("es-MX") , // Spanish (Mexico)
                new CultureInfo("fr-FR"), // French (France)
                new CultureInfo("pt-BR")  // Protuguese (Brazil)
                // Add more languages as needed, matching your .resx file naming
            };

            // Set initial language based on machine's UI culture or a default
            CultureInfo machineCulture = Thread.CurrentThread.CurrentUICulture;
            SelectedLanguage = AvailableLanguages.FirstOrDefault(ci => ci.Name == machineCulture.Name)
                               ?? AvailableLanguages.FirstOrDefault(ci => ci.TwoLetterISOLanguageName == machineCulture.TwoLetterISOLanguageName)
                               ?? AvailableLanguages.First(); // Fallback to first if not found

            // --- Set initial culture for the application ---
            Thread.CurrentThread.CurrentCulture = SelectedLanguage;
            Thread.CurrentThread.CurrentUICulture = SelectedLanguage;

            // Initialize MenuItems for the first time (will use the initial culture)
            MenuItems = new ObservableCollection<MenuItemViewModel>();
            InitializeMenuItems();

            // ReactiveUI subscription to language changes from the ComboBox
            this.WhenAnyValue(x => x.SelectedLanguage)
                .Where(lang => lang != null && lang.Name != Thread.CurrentThread.CurrentUICulture.Name)
                .Subscribe(newLanguage =>
                {
                    Log.Information($"MainViewModel: Changing language to: {newLanguage?.NativeName}");

                    // 1. Update the application's UI culture
                    Thread.CurrentThread.CurrentCulture = newLanguage!;
                    Thread.CurrentThread.CurrentUICulture = newLanguage!;

                    // 2. Re-initialize menu items to update localized strings in the sidebar
                    var previouslySelectedTitle = SelectedMenuItem?.Title;
                    InitializeMenuItems();
                    if (previouslySelectedTitle != null)
                    {
                        var newSelection = FindMenuItemByTitle(MenuItems, previouslySelectedTitle);
                        SelectedMenuItem = newSelection ?? MenuItems.FirstOrDefault();
                    }
                    else
                    {
                        SelectedMenuItem = MenuItems.FirstOrDefault();
                    }

                    // 3. --- PUBLISH THE MESSAGE: Send the LanguageChangedMessage ---
                    MessageBus.Current.SendMessage(new LanguageChangedMessage(newLanguage!));

                    Log.Information($"MainViewModel: Language changed and MessageBus event sent.");
                });

            // Initial navigation to Dashboard
            SelectedMenuItem = MenuItems.FirstOrDefault();
            SelectedMenuItem?.Command?.Execute().Subscribe();
        }

        private void InitializeMenuItems()
        {
            MenuItems.Clear();

            var dashboardMenuItem = new MenuItemViewModel("Overview", NavigateCommand<DashboardViewModel, DashboardView>());

            MenuItems.Add(dashboardMenuItem);
            MenuItems.Add(
                new MenuItemViewModel("Disk Storage", NavigateCommand<DashboardViewModel, DashboardView>())
                {
                    Children =
                    {
                        // Ensure these resource keys exist in your .resx files
                        new MenuItemViewModel(LorealAvaloniaUI.Lang.Resources.DownloadsTitle, NavigateCommand<DownloadViewModel, DownloadView>()),
                        new MenuItemViewModel(LorealAvaloniaUI.Lang.Resources.OfficeFilesCacheTitle, NavigateCommand<OfficeFileCacheViewModel, OfficeFileCacheView>()),
                        new MenuItemViewModel("OneDrive", NavigateCommand<OneDriveViewModel, OneDriveView>()),
                        new MenuItemViewModel("Outlook", NavigateCommand<OutlookFilesViewModel, OutlookFilesView>())
                    }
                }
            );
        }

        private MenuItemViewModel? FindMenuItemByTitle(ObservableCollection<MenuItemViewModel> collection, string title)
        {
            foreach (var item in collection)
            {
                if (item.Title == title) return item;
                if (item.Children != null)
                {
                    var child = FindMenuItemByTitle(item.Children, title);
                    if (child != null) return child;
                }
            }
            return null;
        }

        private ReactiveCommand<Unit, Unit> NavigateCommand<TViewModel, TView>()
            where TViewModel : ReactiveObject
            where TView : Avalonia.Controls.Control, new()
        {
            return ReactiveCommand.Create(() => _navigationService.Navigate<TViewModel, TView>());
        }
    }
}