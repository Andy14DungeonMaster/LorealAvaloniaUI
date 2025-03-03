using System;
using Avalonia.Controls;
using Avalonia.Threading;
using Microsoft.Extensions.DependencyInjection;

namespace LorealAvaloniaUI.Services
{
    public class NavigationService
    {
        private readonly IServiceProvider _serviceProvider;
        private ContentControl? _contentControl;

        public NavigationService(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public void Initialize(ContentControl contentControl)
        {
            _contentControl = contentControl;
        }

        public void Navigate<TViewModel, TView>()
            where TViewModel : class
            where TView : Control, new()
        {
            if (_contentControl == null)
            {
                throw new InvalidOperationException("NavigationService is not initialized with a ContentControl.");
            }

            var viewModel = _serviceProvider.GetService<TViewModel>();

            if (viewModel == null)
            {
                throw new InvalidOperationException($"Could not resolve ViewModel: {typeof(TViewModel).Name}");
            }

            Dispatcher.UIThread.Post(() =>
            {
                var view = new TView { DataContext = viewModel };
                _contentControl.Content = view;
            });
        }
    }
}