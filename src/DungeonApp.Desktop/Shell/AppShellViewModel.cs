using System.Threading.Tasks;
using DungeonApp.Core.Campaigns;
using DungeonApp.Desktop.Features.CampaignLibrary;
using DungeonApp.Desktop.Features.CampaignWorkspace;
using DungeonApp.Desktop.Features.CampaignWorkspace.Layout;
using DungeonApp.Desktop.Shell.Sidebars;
using DungeonApp.Desktop.Shell.StatusBar;
using DungeonApp.Desktop.Shell.TopBar;
using DungeonApp.Desktop.Shell.Workspace;
using DungeonApp.Desktop.ViewModels;

namespace DungeonApp.Desktop.Shell;

/// <summary>
/// Owns which context the GM is in. The Core deliberately has no notion of a currently open
/// campaign - that is shell state, and this is where it lives.
/// </summary>
public sealed class AppShellViewModel : ObservableObject
{
    private const string CampaignsSectionId = "campaigns";
    private const string CampaignsSectionLabel = "Kampanie";

    private readonly WorkspaceLayoutStore _layoutStore;
    private readonly ICampaignRepository _campaigns;
    private readonly CampaignLibraryViewModel _campaignLibrary;

    private object _currentWorkspaceContent;

    /// <summary>
    /// Both null while no campaign is open, and both replaced on every open: a desk belongs to one
    /// campaign, so carrying one instance across campaigns would carry the wrong arrangement and the
    /// wrong panels with it.
    /// </summary>
    private CampaignSession? _openCampaign;
    private CampaignWorkspaceViewModel? _campaignWorkspace;

    public AppShellViewModel(
        WorkspaceLayoutStore layoutStore,
        ICampaignRepository campaigns,
        CreateCampaign createCampaign)
    {
        _layoutStore = layoutStore;
        _campaigns = campaigns;

        TopBar = new TopBarViewModel(CampaignsSectionLabel, new AsyncCommand(CloseCampaignAsync));
        Sidebar = new GlobalSidebarViewModel(OnSectionSelected);
        StatusBar = new StatusBarViewModel("Gotowe");

        _campaignLibrary = new CampaignLibraryViewModel(campaigns, createCampaign, OpenCampaignAsync);

        // Backstage first. The desk is uncovered by opening a campaign, never before.
        _currentWorkspaceContent = _campaignLibrary;
    }

    public TopBarViewModel TopBar { get; }

    public GlobalSidebarViewModel Sidebar { get; }

    public StatusBarViewModel StatusBar { get; }

    public object CurrentWorkspaceContent
    {
        get => _currentWorkspaceContent;
        private set => SetField(ref _currentWorkspaceContent, value);
    }

    /// <summary>Reads the shelf. Kept off the constructor so nothing blocks the first frame.</summary>
    public Task InitializeAsync() => _campaignLibrary.LoadAsync();

    /// <summary>Writes anything the shell has pending. Called from the application's shutdown hooks.</summary>
    public void FlushPendingState() => _campaignWorkspace?.FlushLayout();

    private Task OpenCampaignAsync(Campaign campaign)
    {
        _openCampaign = new CampaignSession(campaign, _campaigns);
        _campaignWorkspace = new CampaignWorkspaceViewModel(_layoutStore, _openCampaign);

        TopBar.ContextTitle = campaign.Name.Value;
        TopBar.IsCampaignOpen = true;
        StatusBar.Message = $"Otwarta kampania: {campaign.Name.Value}";
        CurrentWorkspaceContent = _campaignWorkspace;

        return Task.CompletedTask;
    }

    private async Task CloseCampaignAsync()
    {
        // The desk arrangement is written on the way out, the same as on shutdown.
        _campaignWorkspace?.FlushLayout();

        _openCampaign = null;
        _campaignWorkspace = null;

        TopBar.ContextTitle = CampaignsSectionLabel;
        TopBar.IsCampaignOpen = false;
        StatusBar.Message = "Gotowe";
        CurrentWorkspaceContent = _campaignLibrary;

        // Names and the shelf itself may have moved on while the campaign was open.
        await _campaignLibrary.LoadAsync();
    }

    // Temporary scaffolding until the real context router exists: it dispatches on the section id
    // rather than its label, and every section other than campaigns still lands on the placeholder.
    private void OnSectionSelected(NavigationItemViewModel section)
    {
        if (section.Id != CampaignsSectionId)
        {
            TopBar.ContextTitle = section.Label;
            CurrentWorkspaceContent = new WorkspacePlaceholderViewModel(section.Label);
            return;
        }

        TopBar.ContextTitle = _openCampaign?.Campaign.Name.Value ?? CampaignsSectionLabel;
        CurrentWorkspaceContent = _campaignWorkspace is null
            ? _campaignLibrary
            : _campaignWorkspace;
    }
}
