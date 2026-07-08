using System;
using System.Collections.Generic;
using CommunityToolkit.Mvvm.ComponentModel;
using DungeonApp.ViewModels;

namespace DungeonApp.Services;

public partial class NavigationService : ObservableObject, INavigationService
{
    private readonly Stack<ViewModelBase> _history = new();

    [ObservableProperty]
    private ViewModelBase? _currentView;

    public void NavigateTo(ViewModelBase viewModel)
    {
        if (CurrentView != null)
        {
            _history.Push(CurrentView);
        }
        CurrentView = viewModel;
        CurrentView?.OnNavigatedTo();
    }

    public void NavigateBack()
    {
        if (_history.Count > 0)
        {
            CurrentView = _history.Pop();
        }
        else
        {
            CurrentView = null;
        }
        CurrentView?.OnNavigatedTo();
    }

    public void ClearNavigation()
    {
        _history.Clear();
        CurrentView = null;
    }
}
