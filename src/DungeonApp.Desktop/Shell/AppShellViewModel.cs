using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Threading;
using DungeonApp.Core.Campaigns;
using DungeonApp.Desktop.Features.CampaignLibrary;
using DungeonApp.Desktop.Features.CampaignWorkspace;
using DungeonApp.Desktop.Features.CampaignWorkspace.Layout;
using DungeonApp.Desktop.Shell.Sidebars;
using DungeonApp.Desktop.Shell.StatusBar;
using DungeonApp.Desktop.Shell.TopBar;
using DungeonApp.Desktop.Shell.Workspace;
using DungeonApp.Desktop.Startup;
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
    private readonly CampaignWorkspacePreparationCache _preparations;
    private readonly IStartupStep[] _startupSteps;

    private object _currentWorkspaceContent;
    private bool _isReady;
    private string _startupMessage = "Przygotowywanie biblioteki kampanii…";
    private int _completedSteps;

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
        CampaignLibraryViewModel campaignLibrary,
        CampaignWorkspacePreparationCache preparations,
        IStartupStep[] startupSteps)
    {
        _layoutStore = layoutStore;
        _campaigns = campaigns;
        _preparations = preparations;
        _campaignLibrary = campaignLibrary;
        _startupSteps = startupSteps;

        TopBar = new TopBarViewModel(CampaignsSectionLabel, new AsyncCommand(CloseCampaignAsync));
        Sidebar = new GlobalSidebarViewModel(OnSectionSelected, CloseCampaignAsync);
        StatusBar = new StatusBarViewModel("Gotowe");
        StatusBar.SidebarWidth = Sidebar.SidebarWidth;
        Sidebar.PropertyChanged += OnSidebarPropertyChanged;

        // Backstage first. The desk is uncovered by opening a campaign, never before.
        _currentWorkspaceContent = _campaignLibrary;
    }

    public TopBarViewModel TopBar { get; }

    public GlobalSidebarViewModel Sidebar { get; }

    public StatusBarViewModel StatusBar { get; }

    private void OnSidebarPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(GlobalSidebarViewModel.SidebarWidth))
        {
            StatusBar.SidebarWidth = Sidebar.SidebarWidth;
        }
    }

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

    public int TotalSteps => _startupSteps.Length;

    public int CompletedSteps
    {
        get => _completedSteps;
        private set => SetField(ref _completedSteps, value);
    }

    public object CurrentWorkspaceContent
    {
        get => _currentWorkspaceContent;
        private set => SetField(ref _currentWorkspaceContent, value);
    }

    /// <summary>
    /// Uruchamia jawnie zarejestrowaną sekwencję kroków startowych - dane, potem rozgrzewka
    /// wizualna, krok po kroku, z oddaniem sterowania dispatcherowi między nimi, żeby pasek postępu
    /// zdążył się odświeżyć i żaden krok nie zamroził UI dłużej niż to, co sam rozgrzewa. Awaria
    /// żadnego kroku nie blokuje wejścia do aplikacji - degraduje do zwykłego leniwego wczytywania.
    /// </summary>
    public async Task RunStartupAsync(StartupUiContext ui)
    {
        try
        {
            foreach (var step in _startupSteps)
            {
                StartupMessage = step.Describe();

                await step.PrepareAsync(CancellationToken.None).ConfigureAwait(true);
                await step.ApplyAsync(ui, CancellationToken.None).ConfigureAwait(true);

                CompletedSteps++;

                await Dispatcher.UIThread.InvokeAsync(() => { }, DispatcherPriority.Background);
            }

            CompleteStartup();
        }
        catch (Exception)
        {
            ui.WarmupHost.Content = null;
            CompleteStartupWithWarning();
        }
    }

    private void CompleteStartup()
    {
        StartupMessage = string.Empty;
        IsReady = true;
        StatusBar.Message = "Gotowe";
    }

    private void CompleteStartupWithWarning()
    {
        StartupMessage = string.Empty;
        IsReady = true;
        StatusBar.Message = "Nie udało się przygotować wszystkich widoków. Zostaną wczytane na żądanie.";
    }

    /// <summary>Writes anything the shell has pending. Called from the application's shutdown hooks.</summary>
    public void FlushPendingState() => _campaignWorkspace?.FlushLayout();

    // Internal, nie private: korzeń kompozycji (App.Initialize) domyka na tę metodę wskaźnik
    // zwrotny biblioteki kampanii, zanim ta instancja powłoki w ogóle powstanie.
    internal async Task OpenCampaignAsync(CampaignId id)
    {
        var preparation = await _preparations.TakeAsync(id);
        var campaign = preparation.Campaign;

        _openCampaign = new CampaignSession(campaign, _campaigns);
        _campaignWorkspace = new CampaignWorkspaceViewModel(
            _layoutStore,
            _openCampaign,
            preparation);

        TopBar.ContextTitle = campaign.Name.Value;
        TopBar.IsCampaignOpen = true;
        Sidebar.IsCampaignOpen = true;
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
        Sidebar.IsCampaignOpen = false;
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
