using System.Windows.Input;
using DungeonApp.Desktop.ViewModels;

namespace DungeonApp.Desktop.Shell.TopBar;

/// <summary>
/// The context strip. It reports where the GM is and offers the one action that changes that:
/// leaving an open campaign.
/// </summary>
public sealed class TopBarViewModel(string contextTitle, ICommand closeCampaignCommand) : ObservableObject
{
    private string _contextTitle = contextTitle;
    private bool _isCampaignOpen;

    public string ContextTitle
    {
        get => _contextTitle;
        set => SetField(ref _contextTitle, value);
    }

    /// <summary>
    /// Drives the close action's visibility. Its column is sized Auto, so hiding the action costs
    /// no width and the surrounding chrome keeps its geometry.
    /// </summary>
    public bool IsCampaignOpen
    {
        get => _isCampaignOpen;
        set => SetField(ref _isCampaignOpen, value);
    }

    public ICommand CloseCampaignCommand { get; } = closeCampaignCommand;
}
