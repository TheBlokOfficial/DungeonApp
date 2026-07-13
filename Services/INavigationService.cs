using System.ComponentModel;
using DungeonApp.Models;
using DungeonApp.ViewModels;

namespace DungeonApp.Services;

public interface INavigationService : INotifyPropertyChanged
{
    NavigationIntent CurrentIntent { get; }
    ViewModelBase? CurrentView { get; }
    ViewModelBase? OverlayContent { get; }

    void NavigateTo(ViewModelBase viewModel);
    void NavigateBack();
    void ClearNavigation();

    void ShowOverlay(ViewModelBase viewModel);
    void CloseOverlay();
}
