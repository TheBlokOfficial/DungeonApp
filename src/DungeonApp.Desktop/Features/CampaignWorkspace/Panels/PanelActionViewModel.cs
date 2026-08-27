using System.Windows.Input;

namespace DungeonApp.Desktop.Features.CampaignWorkspace.Panels;

/// <summary>
/// One labelled action offered inside a panel. Shared by the panels that offer a row of fixed
/// choices - a jump forward, a delay - because two of them now want exactly this and nothing more.
/// </summary>
public sealed class PanelActionViewModel(string label, ICommand command)
{
    public string Label { get; } = label;

    public ICommand Command { get; } = command;
}
