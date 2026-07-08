using System.ComponentModel;
using DungeonApp.ViewModels;

namespace DungeonApp.Services;

public interface INavigationService : INotifyPropertyChanged
{
    ViewModelBase? CurrentView { get; }
    void NavigateTo(ViewModelBase viewModel);
    void NavigateBack();
    void ClearNavigation();
}
