using System;
using System.Collections.Generic;
using CommunityToolkit.Mvvm.ComponentModel;
using DungeonApp.Models;
using DungeonApp.ViewModels;

namespace DungeonApp.Services;

public partial class NavigationService : ObservableObject, INavigationService
{
    private readonly Stack<ViewModelBase> _history = new();

    [ObservableProperty]
    private NavigationIntent _currentIntent = NavigationIntent.Parallel;

    [ObservableProperty]
    private ViewModelBase? _currentView;

    [ObservableProperty]
    private ViewModelBase? _overlayContent;

    public void NavigateTo(ViewModelBase viewModel)
    {
        CurrentIntent = NavigationIntent.DrillDown;
        if (CurrentView != null)
        {
            _history.Push(CurrentView);
        }
        
        // Czekamy jeden cykl dyspozytora (Dispatcher.UIThread), by Avalonia
        // zdążyła poprawnie odświeżyć TransitioningContentControl.PageTransition
        // Zanim zmienimy zawartość (CurrentView). Bez tego animacja może zostać
        // pominięta przy pierwszym wczytaniu, co powoduje zacinanie/przeskok.
        Avalonia.Threading.Dispatcher.UIThread.Post(() =>
        {
            CurrentView?.OnNavigatedFrom();
            CurrentView = viewModel;
            CurrentView?.OnNavigatedTo();
        });
    }

    public void NavigateBack()
    {
        CurrentIntent = NavigationIntent.DrillUp;
        var nextView = _history.Count > 0 ? _history.Pop() : null;

        Avalonia.Threading.Dispatcher.UIThread.Post(() =>
        {
            CurrentView?.OnNavigatedFrom();
            CurrentView = nextView;
            CurrentView?.OnNavigatedTo();
        });
    }

    public void ClearNavigation()
    {
        CurrentIntent = NavigationIntent.Parallel;
        _history.Clear();
        CurrentView = null;
    }

    public void ShowOverlay(ViewModelBase viewModel)
    {
        OverlayContent = viewModel;
    }

    public void CloseOverlay()
    {
        OverlayContent = null;
    }
}
