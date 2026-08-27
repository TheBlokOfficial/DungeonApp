using System.Threading.Tasks;
using DungeonApp.Core.Campaigns;
using DungeonApp.Core.Journal;
using DungeonApp.Core.Modules;
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
    private readonly ICampaignJournalStore _journal;
    private readonly CampaignLibraryViewModel _campaignLibrary;
    private readonly CampaignWorkspacePreparationCache _preparations;

    private object _currentWorkspaceContent;
    private bool _isReady;
    private string _startupMessage = "Przygotowywanie biblioteki kampanii…";

    /// <summary>
    /// Both null while no campaign is open, and both replaced on every open: a desk belongs to one
    /// campaign, so carrying one instance across campaigns would carry the wrong arrangement and the
    /// wrong panels with it.
    /// </summary>
    private CampaignSession? _openCampaign;
    private CampaignWorkspaceViewModel? _campaignWorkspace;
    private CampaignId? _warmupCampaignId;

    public AppShellViewModel(
        WorkspaceLayoutStore layoutStore,
        ICampaignRepository campaigns,
        ICampaignJournalStore journal,
        CreateCampaign createCampaign,
        ModuleCatalog modules)
    {
        _layoutStore = layoutStore;
        _campaigns = campaigns;
        _journal = journal;
        _preparations = new CampaignWorkspacePreparationCache(campaigns, journal, layoutStore);

        TopBar = new TopBarViewModel(CampaignsSectionLabel, new AsyncCommand(CloseCampaignAsync));
        Sidebar = new GlobalSidebarViewModel(OnSectionSelected);
        StatusBar = new StatusBarViewModel("Gotowe");

        _campaignLibrary = new CampaignLibraryViewModel(campaigns, createCampaign, modules, OpenCampaignAsync);

        // Backstage first. The desk is uncovered by opening a campaign, never before.
        _currentWorkspaceContent = _campaignLibrary;
    }

    public TopBarViewModel TopBar { get; }

    public GlobalSidebarViewModel Sidebar { get; }

    public StatusBarViewModel StatusBar { get; }

    public bool IsReady
    {
        get => _isReady;
        private set
        {
            if (SetField(ref _isReady, value))
            {
                RaisePropertyChanged(nameof(IsStarting));
            }
        }
    }

    public bool IsStarting => !IsReady;

    public string StartupMessage
    {
        get => _startupMessage;
        private set => SetField(ref _startupMessage, value);
    }

    public object CurrentWorkspaceContent
    {
        get => _currentWorkspaceContent;
        private set => SetField(ref _currentWorkspaceContent, value);
    }

    /// <summary>
    /// Reads and prepares every campaign after the first frame. The view completes startup only
    /// after its own visual warmup, so enabled navigation is a promise that both data and templates
    /// are ready.
    /// </summary>
    public async Task InitializeAsync()
    {
        StartupMessage = "Przygotowywanie biblioteki kampanii…";
        var summaries = await _campaignLibrary.LoadAsync();

        StartupMessage = "Przygotowywanie stołów kampanii…";
        await _preparations.WarmAsync(summaries);

        _warmupCampaignId = summaries.Count > 0 ? summaries[0].Id : null;

        StartupMessage = "Optymalizowanie interfejsu…";
    }

    /// <summary>
    /// Builds the same campaign ViewModel the navigation path will use, but borrows rather than
    /// consumes its prepared data. The view attaches it for one real frame and disposes it before
    /// enabling input.
    /// </summary>
    public async Task<CampaignWorkspaceViewModel?> CreateWorkspaceWarmupAsync()
    {
        if (_warmupCampaignId is not { } id ||
            await _preparations.PeekAsync(id) is not { } preparation)
        {
            return null;
        }

        var session = new CampaignSession(preparation.Campaign, _campaigns, _journal);
        return new CampaignWorkspaceViewModel(_layoutStore, session, preparation);
    }

    public void CompleteStartup()
    {
        StartupMessage = string.Empty;
        IsReady = true;
        StatusBar.Message = "Gotowe";
    }

    public void CompleteStartupWithWarning()
    {
        StartupMessage = string.Empty;
        IsReady = true;
        StatusBar.Message = "Nie udało się przygotować wszystkich widoków. Zostaną wczytane na żądanie.";
    }

    /// <summary>Writes anything the shell has pending. Called from the application's shutdown hooks.</summary>
    public void FlushPendingState() => _campaignWorkspace?.FlushLayout();

    private async Task OpenCampaignAsync(CampaignId id)
    {
        var preparation = await _preparations.TakeAsync(id);
        var campaign = preparation.Campaign;

        _openCampaign = new CampaignSession(campaign, _campaigns, _journal);
        _campaignWorkspace = new CampaignWorkspaceViewModel(
            _layoutStore,
            _openCampaign,
            preparation);

        TopBar.ContextTitle = campaign.Name.Value;
        TopBar.IsCampaignOpen = true;
        StatusBar.Message = $"Otwarta kampania: {campaign.Name.Value}";
        CurrentWorkspaceContent = _campaignWorkspace;
    }

    private async Task CloseCampaignAsync()
    {
        // The desk arrangement is written on the way out, the same as on shutdown.
        _campaignWorkspace?.FlushLayout();
        _campaignWorkspace?.Dispose();

        _openCampaign = null;
        _campaignWorkspace = null;

        TopBar.ContextTitle = CampaignsSectionLabel;
        TopBar.IsCampaignOpen = false;
        StatusBar.Message = "Gotowe";
        CurrentWorkspaceContent = _campaignLibrary;

        // Names and the shelf itself may have moved on while the campaign was open.
        var summaries = await _campaignLibrary.LoadAsync();
        await _preparations.WarmAsync(summaries);
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
