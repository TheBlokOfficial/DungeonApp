using CommunityToolkit.Mvvm.ComponentModel;

namespace DungeonApp.ViewModels;

public abstract class ViewModelBase : ObservableObject
{
    public virtual void OnNavigatedTo() { }
    public virtual void OnNavigatedFrom() { }
}
