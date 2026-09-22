using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Threading;
using DungeonApp.Core.Campaigns;
using DungeonApp.Core.Content;
using DungeonApp.Core.Persistence;
using DungeonApp.Desktop.Content;
using DungeonApp.Desktop.Features.CampaignLibrary;
using DungeonApp.Desktop.Shell.Sidebars;
using DungeonApp.Desktop.Shell.StatusBar;
using DungeonApp.Desktop.Shell.SystemSelection;
using DungeonApp.Desktop.Startup;
using DungeonApp.Desktop.ViewModels;

namespace DungeonApp.Desktop.Shell;

/// <summary>
/// The frame's own view model: the startup curtain, the fullscreen system-selection screen, and -
/// once a system is chosen - the sidebar and whichever content it currently shows. Delegates the
/// actual tab lifecycle to <see cref="ActiveSystemSession"/>, keeping here only what genuinely needs
/// Avalonia's dispatcher (the startup sequence, visual warmup) or is pure screen-routing glue -
/// which is also why, unlike <see cref="ActiveSystemSession"/>, this type has no unit tests of its
/// own (docs/code-map.md already names this the untested half of the shell, for the same reason).
/// </summary>
public sealed class AppShellViewModel : ObservableObject
{
    private readonly ICampaignRepository _campaigns;
    private readonly CampaignLibraryViewModel _campaignLibrary;
    private readonly CampaignPreparationCache _preparations;
    private readonly IStartupStep[] _startupSteps;
    private readonly Func<ContentRegistry> _contentRegistry;

    private object _currentWorkspaceContent;
    private bool _isReady;
    private bool _isSystemChosen;
    private string _startupMessage = "Wczytywanie paczek treści…";
    private int _completedSteps;

    private StartupUiContext? _startupUi;
    private ActiveSystemSession? _session;
    private GlobalSidebarViewModel? _sidebar;
    private CampaignPageViewModel? _campaignPage;

    public AppShellViewModel(
        IReadOnlyList<IGameSystem> systems,
        ICampaignRepository campaigns,
        CampaignLibraryViewModel campaignLibrary,
        CampaignPreparationCache preparations,
        IStartupStep[] startupSteps,
        Func<ContentRegistry> contentRegistry)
    {
        _campaigns = campaigns;
        _campaignLibrary = campaignLibrary;
        _preparations = preparations;
        _startupSteps = startupSteps;
        _contentRegistry = contentRegistry;

        SystemSelection = new SystemSelectionViewModel(systems, ChooseSystemAsync);
        StatusBar = new StatusBarViewModel("Gotowe");

        // Backstage first. Nothing about a system is shown before one is chosen.
        _currentWorkspaceContent = _campaignLibrary;
    }

    public SystemSelectionViewModel SystemSelection { get; }

    public StatusBarViewModel StatusBar { get; }

    public GlobalSidebarViewModel? Sidebar
    {
        get => _sidebar;
        private set => SetField(ref _sidebar, value);
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

    /// <summary>
    /// The fullscreen system picker versus the sidebar-and-content screen (docs/architecture.md,
    /// "Aplikacja startuje na ekranie wyboru systemu"). Both live under the same status bar row -
    /// see AppShellView.axaml.
    /// </summary>
    public bool IsSystemChosen
    {
        get => _isSystemChosen;
        private set => SetField(ref _isSystemChosen, value);
    }

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
    /// Runs the sequence that gates the startup curtain - today, loading content packs only. Nothing
    /// about a campaign or a system is prepared here: docs/tasks.md's "Etap 1" moved that work to
    /// after the GM actually chooses a system (see <see cref="ChooseSystemAsync"/>), since only then
    /// is it known which system's campaign tabs even need warming.
    /// </summary>
    public async Task RunStartupAsync(StartupUiContext ui)
    {
        _startupUi = ui;

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
        StatusBar.Message = "Nie udało się przygotować paczek treści. Zostaną wczytane na żądanie.";
    }

    /// <summary>
    /// Releases every tab this session ever built. Called from the application's shutdown hooks - see
    /// docs/architecture.md's "wyjście z programu" release trigger. A desk tab's own release flushes
    /// whatever arrangement is still pending (the desk's own <c>CampaignDesk</c> entry point),
    /// so this needs no separate layout-flush step of its own the way the previous shell did.
    /// </summary>
    public void FlushPendingState() => _session?.ReleaseAll();

    // Internal, nie private: korzeń kompozycji (App.Initialize) domyka na tę metodę wskaźnik
    // zwrotny biblioteki kampanii, zanim ta instancja powłoki w ogóle powstanie.
    internal async Task OpenCampaignAsync(CampaignId id)
    {
        if (_session is null)
        {
            return;
        }

        var campaign = await _preparations.TakeAsync(id);

        _session.OpenCampaign(campaign);
        _campaignPage = new CampaignPageViewModel(campaign, CloseCampaignAsync);

        Sidebar!.SetCampaignOpen(true, campaign.Name.Value);
        CurrentWorkspaceContent = _campaignPage;
        Sidebar.ActivateCampaignPosition();
        StatusBar.Message = $"Otwarta kampania: {campaign.Name.Value}";
    }

    private async Task CloseCampaignAsync()
    {
        _session?.CloseCampaign();
        _campaignPage = null;

        Sidebar!.SetCampaignOpen(false, campaignName: null);
        CurrentWorkspaceContent = _campaignLibrary;
        Sidebar.ActivateCampaignPosition();
        StatusBar.Message = "Gotowe";

        // Names and the shelf itself may have moved on while the campaign was open.
        var summaries = await _campaignLibrary.LoadAsync();
        await _preparations.WarmAsync(summaries);
    }

    /// <summary>
    /// Applies the GM's choice of system: builds its <see cref="ActiveSystemSession"/> and sidebar,
    /// warms every screen the GM could land on right after this - the shelf, every System-category tab,
    /// and the first campaign's page and tabs - hidden, the same way startup used to warm the desk
    /// (docs/tasks.md's "rama buduje zakładki kampanii dla pierwszej kampanii z półki ukryte i je
    /// zwalnia") - and only then shows the sidebar and the shelf. Before this, the shelf appeared the
    /// instant a system was chosen, unwarmed, which is what made the first real frame visibly jump
    /// (docs/tasks.md). The selection screen simply stays up longer instead. A failure anywhere in here
    /// is a status-bar warning, never a crash - the <c>finally</c> below still lands the GM on a usable,
    /// if emptier, shelf.
    /// </summary>
    private async Task ChooseSystemAsync(IGameSystem system)
    {
        _session = new ActiveSystemSession(system, _contentRegistry, _campaigns);

        var sidebar = new GlobalSidebarViewModel(
            system.SystemTabs,
            system.CampaignTabs,
            ShowCampaignPositionAsync,
            ShowCampaignTabAsync,
            ShowSystemTab,
            ReturnToSelectionAsync);

        try
        {
            var summaries = await _campaignLibrary.LoadAsync();
            await _preparations.WarmAsync(summaries);

            await WarmShelfAsync();
            await WarmSystemTabsAsync(system);

            if (summaries.Count > 0)
            {
                await WarmFirstCampaignAsync(system, summaries[0].Id);
            }
        }
        catch (Exception ex)
        {
            StatusBar.Message = $"Nie udało się w pełni przygotować systemu „{system.DisplayName}”: {ex.Message}";
        }
        finally
        {
            Sidebar = sidebar;
            CurrentWorkspaceContent = _campaignLibrary;
            IsSystemChosen = true;
        }
    }

    /// <summary>
    /// "Zmień system": tears the active system all the way down - closes the open campaign, releases
    /// every tab it or the system ever built - and returns to the selection screen.
    /// </summary>
    private Task ReturnToSelectionAsync()
    {
        _session?.ReleaseAll();
        _session = null;
        _campaignPage = null;
        Sidebar = null;
        IsSystemChosen = false;
        CurrentWorkspaceContent = _campaignLibrary;
        StatusBar.Message = "Gotowe";

        return Task.CompletedTask;
    }

    private Task ShowCampaignPositionAsync()
    {
        CurrentWorkspaceContent = _session is { IsCampaignOpen: true } ? _campaignPage! : _campaignLibrary;
        return Task.CompletedTask;
    }

    private void ShowSystemTab(SystemTabDeclaration declaration)
    {
        try
        {
            CurrentWorkspaceContent = _session!.GetOrCreateSystemTab(declaration).Content;
        }
        catch (Exception ex)
        {
            StatusBar.Message = $"Nie udało się utworzyć zakładki „{declaration.Title}”: {ex.Message}";
        }
    }

    private async Task ShowCampaignTabAsync(CampaignTabDeclaration declaration)
    {
        try
        {
            var content = await _session!.GetOrCreateCampaignTabAsync(declaration);
            CurrentWorkspaceContent = content.Content;
        }
        catch (Exception ex)
        {
            StatusBar.Message = $"Nie udało się utworzyć zakładki „{declaration.Title}”: {ex.Message}";
        }
    }

    /// <summary>
    /// Warms the shelf itself - <see cref="_campaignLibrary"/>'s view, already loaded by the time this
    /// runs. A throwaway <see cref="CampaignLibraryView"/> instance, same as every other warmup step:
    /// the real one the GM lands on is the DataTemplate-built view AppShellView.axaml already declares
    /// for <see cref="CampaignLibraryViewModel"/>, this one only exercises styling and layout once.
    /// </summary>
    private async Task WarmShelfAsync()
    {
        if (_startupUi is not { } ui)
        {
            return;
        }

        try
        {
            await VisualWarmupHost.AttachAndWaitAsync(
                ui.WarmupHost, new CampaignLibraryView { DataContext = _campaignLibrary }, CancellationToken.None);
        }
        catch (Exception)
        {
            // Warmup is an optimization - the real shelf still gets a full chance to build once shown.
        }
    }

    /// <summary>
    /// Warms every System-category tab this system declares, through <see cref="_session"/> itself
    /// rather than a throwaway one: unlike a Campaign-category tab, <see cref="ActiveSystemSession"/>
    /// already builds a System-category tab's content once and keeps it for the rest of the session
    /// (<see cref="ActiveSystemSession.GetOrCreateSystemTab"/>), so warming through the real session
    /// simply makes that one build happen now instead of on first click - there is no separate
    /// build-then-discard step to add, and <see cref="ActiveSystemSession.ReleaseAll"/> already disposes
    /// it exactly when it always did (docs/tasks.md: chosen over a throwaway build because it is the
    /// simpler fit for the lifecycle <see cref="ActiveSystemSession"/> already has).
    /// </summary>
    private async Task WarmSystemTabsAsync(IGameSystem system)
    {
        if (_startupUi is not { } ui)
        {
            return;
        }

        foreach (var declaration in system.SystemTabs)
        {
            try
            {
                var content = _session!.GetOrCreateSystemTab(declaration);
                await VisualWarmupHost.AttachAndWaitAsync(ui.WarmupHost, content.Content, CancellationToken.None);
            }
            catch (Exception)
            {
                // Warmup is an optimization. A tab that fails to warm still gets a real chance to
                // build when the GM actually clicks it - see ShowSystemTab's own try/catch.
            }
        }
    }

    /// <summary>
    /// Warms the first campaign's page, then builds and immediately releases every campaign tab this
    /// system declares, against a throwaway session for <paramref name="warmupCampaignId"/> - never
    /// against <see cref="_session"/>, so nothing built here can leak into the GM's later, real open of
    /// the same campaign. The page uses a throwaway <see cref="CampaignPageViewModel"/> for the same
    /// reason: the real one <see cref="OpenCampaignAsync"/> builds later is what the GM actually acts on.
    /// </summary>
    private async Task WarmFirstCampaignAsync(IGameSystem system, CampaignId warmupCampaignId)
    {
        if (_startupUi is not { } ui || await _preparations.PeekAsync(warmupCampaignId) is not { } campaign)
        {
            return;
        }

        try
        {
            var pageViewModel = new CampaignPageViewModel(campaign, closeCampaign: () => Task.CompletedTask);
            await VisualWarmupHost.AttachAndWaitAsync(
                ui.WarmupHost, new CampaignPageView { DataContext = pageViewModel }, CancellationToken.None);
        }
        catch (Exception)
        {
            // Warmup is an optimization - the real page still gets a full chance to build once opened.
        }

        var warmupSession = new CampaignSession(campaign, _campaigns);
        var warmupContext = new CampaignTabContext(warmupSession, _contentRegistry());

        foreach (var declaration in system.CampaignTabs)
        {
            ITabContent? content = null;

            try
            {
                content = await declaration.CreateContentAsync(warmupContext);
                await VisualWarmupHost.AttachAndWaitAsync(ui.WarmupHost, content.Content, CancellationToken.None);
            }
            catch (Exception)
            {
                // Warmup is an optimization. A tab that fails to warm still gets a real chance to
                // build when the GM actually clicks it - see ShowCampaignTabAsync's own try/catch.
            }
            finally
            {
                content?.Dispose();
            }
        }
    }
}
