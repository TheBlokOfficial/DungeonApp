using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace DungeonApp.Desktop.Shell.TopBar;

/// <summary>
/// The strip above the content area, to the right of the sidebar. Reports where the GM is - which
/// system is active and, once one is open, which campaign - and offers the one action that leaves
/// the system behind: "Zmień system".
/// <para>
/// One instance lives for the whole life of <see cref="AppShellViewModel"/>, updated in place on every
/// system choice and every campaign open/close, rather than rebuilt the way
/// <see cref="Sidebars.GlobalSidebarViewModel"/> is per system - nothing here needs a fresh instance to
/// skip an animation across an already-shown control, because nothing here animates.
/// </para>
/// </summary>
public sealed partial class TopBarViewModel(Func<Task> changeSystem) : ObservableObject
{
    /// <summary>The chosen system's own display name. Empty before any system is chosen - the strip itself stays hidden then (AppShellView.axaml).</summary>
    [ObservableProperty]
    public partial string ActiveSystemName { get; set; } = string.Empty;

    /// <summary>Null while no campaign is open - drives the chevron and this text's own visibility together (<see cref="HasCampaign"/>).</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasCampaign))]
    public partial string? CampaignName { get; set; }

    public bool HasCampaign => CampaignName is not null;

    [RelayCommand]
    private Task ChangeSystem() => changeSystem();
}
