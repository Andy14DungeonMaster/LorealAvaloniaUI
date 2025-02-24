using System;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using ReactiveUI;
using Avalonia.Threading;
using LorealAvaloniaUI.Services;

namespace LorealAvaloniaUI.ViewModels;

public class MainViewModel : ViewModelBase
{
    private readonly NavigationService _navigationService;

    public ReactiveCommand<Unit, Unit> NavigateToDashboardCommand { get; }
    public ReactiveCommand<Unit, Unit> NavigateToSettingsCommand { get; }

    public MainViewModel(NavigationService navigationService)
    {
        _navigationService = navigationService;

        // Force execution on the UI thread
        NavigateToDashboardCommand = ReactiveCommand.Create(
            () => Dispatcher.UIThread.Post(() => _navigationService.Navigate<DashboardViewModel>()),
            outputScheduler: RxApp.MainThreadScheduler  // ✅ Ensures UI thread execution
        );

        NavigateToSettingsCommand = ReactiveCommand.Create(
            () => Dispatcher.UIThread.Post(() => _navigationService.Navigate<SettingsViewModel>()),
            outputScheduler: RxApp.MainThreadScheduler  // ✅ Fixes threading issue
        );
    }
}
