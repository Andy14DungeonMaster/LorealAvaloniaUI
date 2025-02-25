using System;
using System.Reactive;
using System.Threading.Tasks;
using ReactiveUI;
using Avalonia.Threading;
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
            Dispatcher.UIThread.InvokeAsync(() =>
            {
                _navigationService.Navigate<DashboardViewModel, DashboardView>();
            });
        });

        NavigateToSettingsCommand = ReactiveCommand.CreateFromTask(async () =>
 {
     await Task.Run(() =>
     {
         // Simulate background work
     }).ConfigureAwait(true); // Ensures UI thread context is restored

     _navigationService.Navigate<SettingsViewModel, SettingsView>(); // Runs on UI thread
 });

    }
}
