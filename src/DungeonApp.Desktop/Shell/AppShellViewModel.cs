using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Threading;
using DungeonApp.Core.Campaigns;
using DungeonApp.Core.Persistence;
using DungeonApp.Desktop.Content;
using DungeonApp.Desktop.Features.CampaignLibrary;
using DungeonApp.Desktop.Shell.Settings;
using DungeonApp.Desktop.Shell.Sidebars;
using DungeonApp.Desktop.Shell.StatusBar;
using DungeonApp.Desktop.Shell.SystemSelection;
using DungeonApp.Desktop.Shell.TopBar;
using DungeonApp.Desktop.Startup;
using DungeonApp.Desktop.ViewModels;

namespace DungeonApp.Desktop.Shell;

/// <summary>
/// The frame's own view model: the startup curtain, the fullscreen system-selection screen, and -
/// once a system is chosen - the sidebar and whichever content it currently shows. Delegates the
/// actual tab lifecycle to <see cref="ActiveSystemSession"/>, keeping here only what genuinely needs
/// Avalonia's dispatcher (the startup sequence, visual warmup) or is pure screen-routing glue -
/// which is also why, unlike <see cref="ActiveSystemSession"/>, this type has no unit tests of its
/// own (docs/code-state.md already names this the untested half of the shell, for the same reason).
/// </summary>
public sealed class AppShellViewModel : ObservableObject
{
    private readonly ICampaignRepository _campaigns;
    private readonly CampaignLibraryViewModel _campaignLibrary;
    private readonly CampaignPreparationCache _preparations;
    private readonly IStartupStep[] _startupSteps;

    private object _currentWorkspaceContent;
    private bool _isReady;
    private bool _isSystemChosen;
    private string _startupMessage = "Wczytywanie paczek treści…";
    private int _completedSteps;

    private readonly SettingsViewModel _settings = new();

    private ActiveSystemSession? _session;
    private GlobalSidebarViewModel? _sidebar;
    private CampaignPageViewModel? _campaignPage;

    // Survives "Zmień system" and every later choice, per docs/tasks.md zadanie 3: the frame owns
    // collapse, not any one system's sidebar instance. 555802f never wrote this to disk (grep of that
    // commit shows no store call near it), so this stays in-memory only - no new persistence added.
    private bool _sidebarCollapsed;

    public AppShellViewModel(
        IReadOnlyList<IGameSystem> systems,
        ICampaignRepository campaigns,
        CampaignLibraryViewModel campaignLibrary,
        CampaignPreparationCache preparations,
        IStartupStep[] startupSteps)
    {
        _campaigns = campaigns;
        _campaignLibrary = campaignLibrary;
        _preparations = preparations;
        _startupSteps = startupSteps;

        SystemSelection = new SystemSelectionViewModel(systems, ChooseSystemAsync);
        StatusBar = new StatusBarViewModel("Gotowe");
        // One instance for the app's whole life (unlike Sidebar, never rebuilt per system) - see
        // TopBarViewModel's own remarks. "Zmień system" is the same action the sidebar used to
        // trigger, wired here instead.
        TopBar = new TopBarViewModel(new AsyncCommand(ReturnToSelectionAsync));

        // Backstage first. Nothing about a system is shown before one is chosen.
        _currentWorkspaceContent = _campaignLibrary;
    }

    public SystemSelectionViewModel SystemSelection { get; }

    public StatusBarViewModel StatusBar { get; }

    public TopBarViewModel TopBar { get; }

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
    /// Runs the whole startup sequence behind the curtain: content packs, the campaign shelf, and -
    /// per docs/tasks.md's revised zadanie 1 - every visual warmup the GM's first minute could
    /// otherwise pay for on click (every compiled system's tabs, cards and desk, the sidebar in both
    /// collapse states, the shelf and the campaign page). Nothing here is bounded by how long it
    /// takes - a slower, fully warmed curtain is the point, not a cost to shave.
    /// </summary>
    public async Task RunStartupAsync(StartupUiContext ui)
    {
        var stopwatch = Stopwatch.StartNew();
        IStartupStep? currentStep = null;

        try
        {
            foreach (var step in _startupSteps)
            {
                currentStep = step;
                StartupMessage = step.Describe();

                var stepWatch = Stopwatch.StartNew();

                await step.PrepareAsync(CancellationToken.None).ConfigureAwait(true);
                await step.ApplyAsync(ui, CancellationToken.None).ConfigureAwait(true);

                Debug.WriteLine($"[Startup] {step.GetType().Name}: {stepWatch.ElapsedMilliseconds} ms");

                CompletedSteps++;

                await Dispatcher.UIThread.InvokeAsync(() => { }, DispatcherPriority.Background);
            }

            CompleteStartup();
        }
        catch (Exception)
        {
            ui.WarmupHost.Content = null;
            CompleteStartupWithWarning(currentStep?.FailureWarning);
        }
        finally
        {
            Debug.WriteLine($"[Startup] Cały start: {stopwatch.ElapsedMilliseconds} ms");
        }
    }

    private void CompleteStartup()
    {
        StartupMessage = string.Empty;
        IsReady = true;
        StatusBar.Message = "Gotowe";
    }

    /// <summary>
    /// <paramref name="warning"/> comes from whichever step failed (<see cref="IStartupStep.FailureWarning"/>)
    /// - the frame never knows why a step failed, only that one did, so the step itself is the only
    /// one that can put that into words (docs/architecture.md, "Start aplikacji").
    /// </summary>
    private void CompleteStartupWithWarning(string? warning)
    {
        StartupMessage = string.Empty;
        IsReady = true;
        StatusBar.Message = warning ?? "Nie udało się w pełni przygotować startu aplikacji. Zostanie uruchomiona mimo to.";
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
    internal async Task OpenCampaignAsync(CampaignSummary summary)
    {
        if (_session is null)
        {
            return;
        }

        var campaign = await _preparations.TakeAsync(summary);

        _session.OpenCampaign(campaign);
        _campaignPage = new CampaignPageViewModel(campaign, CloseCampaignAsync);

        Sidebar!.SetCampaignOpen(true, campaign.Name.Value);
        CurrentWorkspaceContent = _campaignPage;
        Sidebar.ActivateCampaignPosition();
        StatusBar.Message = $"Otwarta kampania: {campaign.Name.Value}";
        TopBar.CampaignName = campaign.Name.Value;
    }

    private async Task CloseCampaignAsync()
    {
        _session?.CloseCampaign();
        _campaignPage = null;

        Sidebar!.SetCampaignOpen(false, campaignName: null);
        CurrentWorkspaceContent = _campaignLibrary;
        Sidebar.ActivateCampaignPosition();
        StatusBar.Message = "Gotowe";
        TopBar.CampaignName = null;

        // Names and the shelf itself may have moved on while the campaign was open.
        var summaries = await _campaignLibrary.LoadAsync();
        await _preparations.WarmAsync(summaries);
    }

    /// <summary>
    /// Applies the GM's choice of system. Everything expensive - every compiled system's tabs, cards
    /// and desk, the shelf, the campaign page, the sidebar chrome in both collapse states - already
    /// ran once behind the startup curtain (<see cref="RunStartupAsync"/>, docs/tasks.md's revised
    /// zadanie 1); nothing here builds a type warmup has not already shown once. What is left is
    /// cheap and depends only on which system was picked: the session that will lazily build this
    /// system's *real*, cached tab content on first click, and a sidebar instance carrying this
    /// system's own tab declarations and the collapse state the frame remembers across systems (zadanie
    /// 3). Also the point where the shelf learns which system is now active
    /// (<see cref="CampaignLibraryViewModel.SetActiveSystem"/>) and reloads to filter itself to it -
    /// docs/architecture.md, "Kampania należy do jednego systemu": the load that already ran behind
    /// the startup curtain, before any system was chosen, showed every campaign unfiltered.
    /// </summary>
    private async Task ChooseSystemAsync(IGameSystem system)
    {
        _session = new ActiveSystemSession(system, _campaigns);

        var sidebar = new GlobalSidebarViewModel(
            system.SystemTabs,
            system.CampaignTabs,
            ShowCampaignPositionAsync,
            ShowCampaignTabAsync,
            ShowSystemTab,
            ShowSettingsAsync,
            startCollapsed: _sidebarCollapsed);

        sidebar.PropertyChanged += OnSidebarPropertyChanged;

        Sidebar = sidebar;
        CurrentWorkspaceContent = _campaignLibrary;
        IsSystemChosen = true;
        TopBar.ActiveSystemName = system.DisplayName;
        TopBar.CampaignName = null;

        _campaignLibrary.SetActiveSystem(system);
        await _campaignLibrary.LoadAsync();
    }

    /// <summary>
    /// "Zmień system": tears the active system all the way down - closes the open campaign, releases
    /// every tab it or the system ever built - and returns to the selection screen. Unsubscribes from
    /// the outgoing sidebar's collapse notifications first - <see cref="_sidebarCollapsed"/> itself,
    /// the value that survives into the next system's sidebar, is left untouched.
    /// </summary>
    private Task ReturnToSelectionAsync()
    {
        _session?.ReleaseAll();
        _session = null;
        _campaignPage = null;

        if (Sidebar is { } outgoing)
        {
            outgoing.PropertyChanged -= OnSidebarPropertyChanged;
        }

        Sidebar = null;
        IsSystemChosen = false;
        CurrentWorkspaceContent = _campaignLibrary;
        StatusBar.Message = "Gotowe";
        TopBar.ActiveSystemName = string.Empty;
        TopBar.CampaignName = null;

        return Task.CompletedTask;
    }

    /// <summary>Mirrors the active sidebar's collapse state into the frame so it outlives that sidebar instance (zadanie 3).</summary>
    private void OnSidebarPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(GlobalSidebarViewModel.IsCollapsed) && sender is GlobalSidebarViewModel sidebar)
        {
            _sidebarCollapsed = sidebar.IsCollapsed;
        }
    }

    private Task ShowCampaignPositionAsync()
    {
        CurrentWorkspaceContent = _session is { IsCampaignOpen: true } ? _campaignPage! : _campaignLibrary;
        return Task.CompletedTask;
    }

    /// <summary>"Ustawienia": a frame-owned position, available with or without an open campaign. Shows the empty tab - see <see cref="SettingsViewModel"/>'s own remarks.</summary>
    private Task ShowSettingsAsync()
    {
        CurrentWorkspaceContent = _settings;
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
}
