using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace DungeonApp.ViewModels.Dashboard;

public partial class PropertyFilterGroupViewModel : ViewModelBase
{
    public string PropertyKey { get; }
    public string DisplayName { get; }
    public ObservableCollection<FilterItemViewModel> Values { get; } = new();

    public PropertyFilterGroupViewModel(string propertyKey, string displayName)
    {
        PropertyKey = propertyKey;
        DisplayName = displayName;
    }
}
