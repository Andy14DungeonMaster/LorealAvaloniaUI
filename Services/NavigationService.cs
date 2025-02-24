using System;
using Avalonia.Controls;
using Microsoft.Extensions.DependencyInjection;

namespace LorealAvaloniaUI.Services;

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

    public void Navigate<TViewModel>() where TViewModel : class
    {
        var viewModel = _serviceProvider.GetService<TViewModel>();
        if (_contentControl != null && viewModel != null)
        {
            _contentControl.DataContext = viewModel;
        }
    }
}
