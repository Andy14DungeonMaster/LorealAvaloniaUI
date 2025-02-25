using System;
using System.Reactive;
using ReactiveUI;
using LorealAvaloniaUI.Services;
using LorealAvaloniaUI.Views;

namespace LorealAvaloniaUI.ViewModels;

public class MainViewModel : ViewModelBase
{
    private readonly NavigationService _navigationService;

    public ReactiveCommand<Unit, Unit> NavigateToDashboardCommand { get; }
    public ReactiveCommand<Unit, Unit> NavigateToSettingsCommand { get; }

    public MainViewModel(NavigationService navigationService)
    {
        _navigationService = navigationService;

        NavigateToDashboardCommand = ReactiveCommand.Create(() =>
        {
            Console.WriteLine("🚀 Navigating to Dashboard");
            _navigationService.Navigate<DashboardViewModel, DashboardView>();
        });

        NavigateToSettingsCommand = ReactiveCommand.Create(() =>
        {
            Console.WriteLine("⚙️ Navigating to Settings");
            _navigationService.Navigate<SettingsViewModel, SettingsView>();
        });
    }
}
