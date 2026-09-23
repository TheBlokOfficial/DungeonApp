using System.Windows.Input;
using DungeonApp.Desktop.ViewModels;

namespace DungeonApp.Desktop.Shell.TopBar;

/// <summary>
/// The strip above the content area, to the right of the sidebar. Reports where the GM is - which
/// system is active and, once one is open, which campaign - and offers the one action that leaves
/// the system behind: "Zmień system", the same action the sidebar used to carry.
/// <para>
/// One instance lives for the whole life of <see cref="AppShellViewModel"/>, updated in place on every
/// system choice and every campaign open/close, rather than rebuilt the way
/// <see cref="Sidebars.GlobalSidebarViewModel"/> is per system - nothing here needs a fresh instance to
/// skip an animation across an already-shown control, because nothing here animates.
/// </para>
/// </summary>
public sealed class TopBarViewModel(ICommand changeSystemCommand) : ObservableObject
{
    private string _activeSystemName = string.Empty;
    private string? _campaignName;

    /// <summary>The chosen system's own display name. Empty before any system is chosen - the strip itself stays hidden then (AppShellView.axaml).</summary>
    public string ActiveSystemName
    {
        get => _activeSystemName;
        set => SetField(ref _activeSystemName, value);
    }

    /// <summary>Null while no campaign is open - drives the chevron and this text's own visibility together (<see cref="HasCampaign"/>).</summary>
    public string? CampaignName
    {
        get => _campaignName;
        set
        {
            if (SetField(ref _campaignName, value))
            {
                RaisePropertyChanged(nameof(HasCampaign));
            }
        }
    }

    public bool HasCampaign => CampaignName is not null;

    public ICommand ChangeSystemCommand { get; } = changeSystemCommand;
}
