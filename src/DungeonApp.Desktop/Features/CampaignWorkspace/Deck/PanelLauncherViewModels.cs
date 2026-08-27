using System.Collections.Generic;
using System.Windows.Input;
using DungeonApp.Desktop.Features.CampaignWorkspace.Panels;
using DungeonApp.Desktop.ViewModels;

namespace DungeonApp.Desktop.Features.CampaignWorkspace.Deck;

/// <summary>
/// One tool offered by the deck. Built from the catalogue, so the deck cannot drift out of step with
/// what the workspace can actually open.
/// </summary>
public sealed class PanelLauncherItemViewModel(WorkspacePanelDescriptor descriptor, ICommand openCommand)
    : ObservableObject
{
    private bool _isOpen;

    public WorkspacePanelDescriptor Descriptor { get; } = descriptor;

    public string Title { get; } = descriptor.Title;

    public string IconResourceKey { get; } = descriptor.IconResourceKey;

    public ICommand OpenCommand { get; } = openCommand;

    /// <summary>Already on the desk. The entry stays usable - it just brings the panel forward.</summary>
    public bool IsOpen
    {
        get => _isOpen;
        set => SetField(ref _isOpen, value);
    }
}

/// <summary>
/// Groups in the launcher use the campaign navigation headings from the mockups. The grouping moved
/// from the sidebar to the deck; the vocabulary did not change.
/// </summary>
public sealed record PanelLauncherGroupViewModel(string Label, IReadOnlyList<PanelLauncherItemViewModel> Items);
