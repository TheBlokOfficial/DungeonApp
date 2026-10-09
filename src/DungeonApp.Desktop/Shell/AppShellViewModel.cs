using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Threading;
using DungeonApp.Core.Campaigns;
using DungeonApp.Core.Persistence;
using DungeonApp.Core.Systems;
using DungeonApp.Desktop.Diagnostics;
using DungeonApp.Desktop.Systems;
using DungeonApp.Desktop.Features.CampaignLibrary;
using DungeonApp.Desktop.Shell.Gallery;
using DungeonApp.Desktop.Shell.Settings;
using DungeonApp.Desktop.Shell.Sidebars;
using DungeonApp.Desktop.Shell.StatusBar;
using DungeonApp.Desktop.Shell.SystemSelection;
using DungeonApp.Desktop.Shell.TopBar;
using DungeonApp.Desktop.Startup;
using DungeonApp.Desktop.Workspace.Layout;
using CommunityToolkit.Mvvm.ComponentModel;

namespace DungeonApp.Desktop.Shell;

/// <summary>
/// The frame's own view model: the startup curtain, the fullscreen system-selection screen, and -
/// once a system is chosen - the sidebar and whichever content it currently shows. Delegates the
/// actual tab lifecycle to <see cref="ActiveSystemSession"/>, keeping here only what genuinely needs
/// Avalonia's dispatcher (the startup sequence, visual warmup) or is pure screen-routing glue -
/// which is also why, unlike <see cref="ActiveSystemSession"/>, this type has no unit tests of its
/// own.
/// </summary>
public sealed partial class AppShellViewModel : ObservableObject
{
    private readonly IReadOnlyDictionary<SystemId, ICampaignRepository> _repositoriesBySystem;
    private readonly CampaignLibraryViewModel _campaignLibrary;
    private readonly CampaignPreparationCache _preparations;
    private readonly IStartupStep[] _startupSteps;

    private readonly GalleryViewModel _gallery = new();
    private readonly SettingsViewModel _settings = new();

    private readonly WorkspaceLayoutStore _layoutStore;

    private ActiveSystemSession? _session;

    // Survives "Zmień system" and every later choice: the frame owns collapse, not any one system's
    // sidebar instance. In memory only - never written to disk.
    private bool _sidebarCollapsed;

    public AppShellViewModel(
        IReadOnlyList<IGameSystem> systems,
        IReadOnlyDictionary<SystemId, ICampaignRepository> repositoriesBySystem,
        CampaignLibraryViewModel campaignLibrary,
        CampaignPreparationCache preparations,
        WorkspaceLayoutStore layoutStore,
        IStartupStep[] startupSteps)
    {
        _layoutStore = layoutStore;
        _repositoriesBySystem = repositoriesBySystem;
        _campaignLibrary = campaignLibrary;
        _preparations = preparations;
        _startupSteps = startupSteps;

        SystemSelection = new SystemSelectionViewModel(systems, ChooseSystemAsync);
        StatusBar = new StatusBarViewModel("Gotowe");
        // One instance for the app's whole life (unlike Sidebar, never rebuilt per system) - see
        // TopBarViewModel's own remarks. "Zmień system" is wired here.
        TopBar = new TopBarViewModel(ReturnToSelectionAsync);

        // Backstage first. Nothing about a system is shown before one is chosen.
        CurrentWorkspaceContent = _campaignLibrary;
    }

    public SystemSelectionViewModel SystemSelection { get; }

    public StatusBarViewModel StatusBar { get; }

    public TopBarViewModel TopBar { get; }

    [ObservableProperty]
    public partial GlobalSidebarViewModel? Sidebar { get; private set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsStarting))]
    public partial bool IsReady { get; private set; }

    public bool IsStarting => !IsReady;

    /// <summary>
    /// The fullscreen system picker, where the application starts, versus the sidebar-and-content
    /// screen. Both live under the same status bar row - see AppShellView.axaml.
    /// </summary>
    [ObservableProperty]
    public partial bool IsSystemChosen { get; private set; }

    [ObservableProperty]
    public partial string StartupMessage { get; private set; } = "Wczytywanie paczek treści…";

    /// <summary>
    /// Raised when a change of the open campaign did not reach the disk, whichever tool made it. The
    /// view shows it; nothing is written in response.
    /// </summary>
    public event Action<string>? SaveFailed;

    public int TotalSteps => _startupSteps.Length;

    [ObservableProperty]
    public partial int CompletedSteps { get; private set; }

    [ObservableProperty]
    public partial object CurrentWorkspaceContent { get; private set; }

    /// <summary>
    /// Runs the whole startup sequence behind the curtain: content packs, the campaign shelf, and
    /// every visual warmup the GM's first minute could otherwise pay for on click (every compiled
    /// system's tabs, cards and desk, the sidebar in both collapse states and the shelf). Nothing here is bounded by how long it takes - a slower, fully warmed
    /// curtain is the point, not a cost to shave.
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
        catch (Exception ex)
        {
            AppLog.Error($"Krok startowy {currentStep?.GetType().Name} zawiódł; start dokończony bez reszty kroków.", ex);
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
    /// one that can put that into words.
    /// </summary>
    private void CompleteStartupWithWarning(string? warning)
    {
        StartupMessage = string.Empty;
        IsReady = true;
        StatusBar.Message = warning ?? "Nie udało się w pełni przygotować startu aplikacji. Zostanie uruchomiona mimo to.";
    }

    /// <summary>
    /// Releases the desk and every tab this session ever built. Called from the application's
    /// shutdown hooks: exiting the program is a release point. Releasing the desk flushes whatever
    /// arrangement is still pending (the <c>CampaignDesk</c> entry point), so this needs no separate
    /// layout-flush step of its own.
    /// </summary>
    public void FlushPendingState() => _session?.ReleaseAll();

    // Internal, not private: the composition root (App.Initialize) closes the campaign-library
    // callback over this method before this shell instance is created.
    internal async Task OpenCampaignAsync(CampaignSummary summary)
    {
        if (_session is null)
        {
            return;
        }

        var campaign = await _preparations.TakeAsync(summary);

        _session.OpenCampaign(campaign);
        var desk = await _session.GetOrCreateDeskAsync(CloseCampaignAsync);

        Sidebar!.SetCampaignOpen(true);
        CurrentWorkspaceContent = desk.Content;
        Sidebar.ActivateCampaignPosition();
        StatusBar.Message = $"Otwarta kampania: {campaign.Name.Value}";
        TopBar.CampaignName = campaign.Name.Value;
    }

    private async Task CloseCampaignAsync()
    {
        _session?.CloseCampaign();

        Sidebar!.SetCampaignOpen(false);
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
    /// and desk, the shelf, the sidebar chrome in both collapse states - already
    /// ran once behind the startup curtain (<see cref="RunStartupAsync"/>); nothing here builds a
    /// type warmup has not already shown once. What is left is cheap and depends only on which
    /// system was picked: the session that will lazily build this system's *real*, cached tab
    /// content on first click, and a sidebar instance carrying this system's own tab declarations
    /// and the collapse state the frame remembers across systems. Also the point where the shelf
    /// learns which system is now active (<see cref="CampaignLibraryViewModel.SetActiveSystem"/>)
    /// and reloads to filter itself to it, since a campaign belongs to one system: the load that
    /// already ran behind the startup curtain, before any system was chosen, showed every campaign
    /// unfiltered.
    /// </summary>
    private async Task ChooseSystemAsync(IGameSystem system)
    {
        _session = new ActiveSystemSession(system, _repositoriesBySystem[system.Id], _layoutStore);
        _session.SaveFailed += warning => SaveFailed?.Invoke(warning);

        var sidebar = new GlobalSidebarViewModel(
            system.SystemTabs,
            system.CampaignTabs,
            ShowCampaignPositionAsync,
            ShowCampaignTabAsync,
            ShowSystemTab,
            ShowGalleryAsync,
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

    /// <summary>Mirrors the active sidebar's collapse state into the frame so it outlives that
    /// sidebar instance.</summary>
    private void OnSidebarPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(GlobalSidebarViewModel.IsCollapsed) && sender is GlobalSidebarViewModel sidebar)
        {
            _sidebarCollapsed = sidebar.IsCollapsed;
        }
    }

    private async Task ShowCampaignPositionAsync()
    {
        CurrentWorkspaceContent = _session is { IsCampaignOpen: true } session
            ? (await session.GetOrCreateDeskAsync(CloseCampaignAsync)).Content
            : _campaignLibrary;
    }

    /// <summary>"Galeria kontrolek": a frame-owned position, available with or without an open campaign - see <see cref="GalleryViewModel"/>'s own remarks.</summary>
    private Task ShowGalleryAsync()
    {
        CurrentWorkspaceContent = _gallery;
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
            AppLog.Error($"Nie udało się utworzyć zakładki {declaration.Id}.", ex);
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
            AppLog.Error($"Nie udało się utworzyć zakładki {declaration.Id}.", ex);
            StatusBar.Message = $"Nie udało się utworzyć zakładki „{declaration.Title}”: {ex.Message}";
        }
    }
}
