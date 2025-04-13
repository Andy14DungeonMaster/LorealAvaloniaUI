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
                Console.WriteLine("ConfirmCommand executed");
                return true;
            });
            CancelCommand = ReactiveCommand.Create(() =>
            {
                Console.WriteLine("CancelCommand executed");
                return false;
            });
        }

        public static async Task<bool> ShowAsync(Window? parent, string message)
        {
            var dialog = new ConfirmationDialog();
            var viewModel = new ConfirmationDialogViewModel(dialog) { Message = message };
            dialog.DataContext = viewModel;

            Console.WriteLine($"ShowAsync called with message: {message}, parent: {(parent == null ? "null" : "non-null")}");

            if (parent == null || parent == dialog || !parent.IsVisible)
            {
                dialog.WindowStartupLocation = WindowStartupLocation.CenterScreen;
                var tcs = new TaskCompletionSource<bool>();
                bool resultSet = false;

                var confirmSubscription = viewModel.ConfirmCommand
                    .Subscribe(result =>
                    {
                        Console.WriteLine($"Confirm result: {result}");
                        resultSet = true;
                        tcs.TrySetResult(result);
                        dialog.Close();
                    });

                var cancelSubscription = viewModel.CancelCommand
                    .Subscribe(result =>
                    {
                        Console.WriteLine($"Cancel result: {result}");
                        resultSet = true;
                        tcs.TrySetResult(result);
                        dialog.Close();
                    });

                dialog.Closed += (s, e) =>
                {
                    Console.WriteLine($"Dialog closed, resultSet: {resultSet}");
                    if (!resultSet && !tcs.Task.IsCompleted)
                    {
                        Console.WriteLine("Setting default result to false");
                        tcs.TrySetResult(false);
                    }
                    confirmSubscription.Dispose();
                    cancelSubscription.Dispose();
                };

                dialog.Show();
                var result = await tcs.Task;
                Console.WriteLine($"ShowAsync returning: {result}");
                return result;
            }

            var dialogResult = await dialog.ShowDialog<bool>(parent);
            Console.WriteLine($"ShowDialog returning: {dialogResult}");
            return dialogResult;
        }
    }
}