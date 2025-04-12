using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using ReactiveUI;
using System;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;

namespace LorealAvaloniaUI.Views
{
    public partial class ConfirmationDialog : Window
    {
        public ConfirmationDialog()
        {
            InitializeComponent();
            DataContext = new ConfirmationDialogViewModel(this);
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }

    public class ConfirmationDialogViewModel : ReactiveObject
    {
        private readonly Window _dialog;
        private string _message = string.Empty;

        public string Message
        {
            get => _message;
            set => this.RaiseAndSetIfChanged(ref _message, value);
        }

        public ReactiveCommand<Unit, bool> ConfirmCommand { get; }
        public ReactiveCommand<Unit, bool> CancelCommand { get; }

        public ConfirmationDialogViewModel(Window dialog)
        {
            _dialog = dialog ?? throw new ArgumentNullException(nameof(dialog));
            ConfirmCommand = ReactiveCommand.Create(() =>
            {
                _dialog.Close();
                return true;
            });
            CancelCommand = ReactiveCommand.Create(() =>
            {
                _dialog.Close();
                return false;
            });
        }

        public static async Task<bool> ShowAsync(Window? parent, string message)
        {
            var dialog = new ConfirmationDialog();
            var viewModel = new ConfirmationDialogViewModel(dialog) { Message = message };
            dialog.DataContext = viewModel;

            if (parent == null || parent == dialog || !parent.IsVisible)
            {
                dialog.WindowStartupLocation = WindowStartupLocation.CenterScreen;
                var tcs = new TaskCompletionSource<bool>();

                // Subscribe to commands to capture result
                var confirmSubscription = viewModel.ConfirmCommand
                    .Subscribe(result => tcs.TrySetResult(result));
                var cancelSubscription = viewModel.CancelCommand
                    .Subscribe(result => tcs.TrySetResult(result));

                // Handle window close without button press (e.g., X button)
                dialog.Closed += (s, e) =>
                {
                    if (!tcs.Task.IsCompleted)
                        tcs.TrySetResult(false); // Default to false if closed without action
                };

                try
                {
                    dialog.Show();
                    return await tcs.Task;
                }
                finally
                {
                    // Clean up subscriptions
                    confirmSubscription.Dispose();
                    cancelSubscription.Dispose();
                }
            }

            return await dialog.ShowDialog<bool>(parent);
        }
    }
}