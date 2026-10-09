using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace DungeonApp.Desktop.Workspace.Leaf;

/// <summary>
/// The desk's command strip. It knows nothing of the campaign or the system: closing is handed in as a
/// delegate. Undo, redo and the command palette are placeholders the view shows disabled.
/// </summary>
public sealed partial class DeskLeafViewModel(Func<Task> closeCampaign) : ObservableObject
{
    [RelayCommand]
    private Task CloseCampaign() => closeCampaign();
}
