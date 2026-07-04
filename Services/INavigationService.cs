using DungeonApp.ViewModels;

namespace DungeonApp.Services;

public interface INavigationService
{
    ViewModelBase? CurrentView { get; }
    void NavigateTo(ViewModelBase viewModel);
    void NavigateBack();
}
