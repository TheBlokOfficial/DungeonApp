using System;
using System.ComponentModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Avalonia.Animation;
using DungeonApp.Models;
using DungeonApp.Services;
using DungeonApp.UI;
using DungeonApp.ViewModels.Dashboard;
using Microsoft.Extensions.DependencyInjection;

namespace DungeonApp.ViewModels;

public enum RegistryTab { Characters, Items, Adversaries }

public partial class MainWindowViewModel : ViewModelBase
{
    private readonly ISettingsService _settingsService;
    public INavigationService NavigationService { get; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CurrentSidebarWidth))]
    private bool _isSidebarExpanded = true;

    public double CurrentSidebarWidth => IsSidebarExpanded ? 260 : 74;


    private ViewModelBase? _dashboardCurrentView;
    public ViewModelBase? DashboardCurrentView
    {
        get => _dashboardCurrentView;
        set
        {
            if (SetProperty(ref _dashboardCurrentView, value))
            {
                OnPropertyChanged(nameof(ActiveMainView));
                OnPropertyChanged(nameof(IsCampaignsActive));
                OnPropertyChanged(nameof(IsSettingsActive));
                OnPropertyChanged(nameof(IsCharactersActive));
                OnPropertyChanged(nameof(IsItemsActive));
                OnPropertyChanged(nameof(IsAdversariesActive));
                
                _dashboardCurrentView?.OnNavigatedTo();
            }
        }
    }

    public bool IsCampaignsActive => DashboardCurrentView is CampaignsTabViewModel;
    public bool IsSettingsActive => DashboardCurrentView is SettingsTabViewModel;
    public bool IsCharactersActive => DashboardCurrentView is CharactersTabViewModel;
    public bool IsItemsActive => DashboardCurrentView is ItemsTabViewModel;
    public bool IsAdversariesActive => DashboardCurrentView is AdversariesTabViewModel;

    public ViewModelBase? ActiveMainView => NavigationService.CurrentView ?? DashboardCurrentView;

    public bool IsDashboardVisible => NavigationService.CurrentView == null;

    public ViewModelBase? OverlayContent => NavigationService.OverlayContent;
    public bool IsOverlayVisible => OverlayContent != null;

    [ObservableProperty]
    private IPageTransition? _currentPageTransition;

    /// <summary>
    /// Flaga gotowości aplikacji — false = ekran ładowania, true = główny interfejs.
    /// </summary>
    [ObservableProperty]
    private bool _isAppReady;

    /// <summary>
    /// Postęp ładowania w procentach (0.0 – 100.0) wyświetlany na ekranie startowym.
    /// </summary>
    [ObservableProperty]
    private double _loadingProgress;

    [ObservableProperty]
    private string _loadingStatus = "Inicjalizacja...";

    [ObservableProperty]
    private double _uiScale = 1.0;
    
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ScaledMinWidth))]
    [NotifyPropertyChangedFor(nameof(ScaledMinHeight))]
    private double _effectiveUiScale = 1.0;

    public double ScaledMinWidth => 900 * EffectiveUiScale;
    public double ScaledMinHeight => 600 * EffectiveUiScale;

    public MainWindowViewModel(
        INavigationService navigationService,
        ISettingsService settingsService)
    {
        NavigationService = navigationService;
        _settingsService = settingsService;

        ((System.ComponentModel.INotifyPropertyChanged)NavigationService).PropertyChanged += (s, e) => 
        {
            if (e.PropertyName == nameof(NavigationService.CurrentIntent))
            {
                UpdateTransition(NavigationService.CurrentIntent);
            }
            else if (e.PropertyName == nameof(NavigationService.CurrentView))
            {
                OnPropertyChanged(nameof(IsDashboardVisible));
                OnPropertyChanged(nameof(ActiveMainView));
                
                // Po powrocie z nawigacji (np. z CampaignDetail), CurrentView wraca na null.
                // DashboardCurrentView się nie zmienił (ten sam Singleton), więc jego setter
                // nie odpali OnNavigatedTo. Musimy to zrobić ręcznie, żeby dane się odświeżyły.
                if (NavigationService.CurrentView == null)
                {
                    _dashboardCurrentView?.OnNavigatedTo();
                }
            }
            else if (e.PropertyName == nameof(NavigationService.OverlayContent))
            {
                OnPropertyChanged(nameof(OverlayContent));
                OnPropertyChanged(nameof(IsOverlayVisible));
            }
        };

        var settings = _settingsService.LoadSettings();
        UiScale = settings.UiScale;

        _settingsService.SettingsChanged += () =>
        {
            var newSettings = _settingsService.LoadSettings();
            UiScale = newSettings.UiScale;
        };

        // Ekran ładowania — rozgrzewamy wszystkie widoki ZANIM użytkownik zobaczy interfejs.
        _ = InitializeAppAsync();
    }

    /// <summary>
    /// Główna sekwencja startowa aplikacji: rozgrzewanie widoków → pokazanie interfejsu.
    /// </summary>
    /// <remarks>
    /// DLACZEGO faktyczna nawigacja zamiast ręcznego Build+Measure:
    /// Ręczne Build + DataContext + Measure/Arrange NIE wystarczało do eliminacji stutterów,
    /// bo widok budowany poza drzewem wizualnym nie przechodzi przez pełny cykl życia Avalonii:
    /// OnAttachedToVisualTree, template application, rozwiązanie DynamicResource, itp.
    /// 
    /// Rozwiązanie: Dashboard jest ZAWSZE w drzewie wizualnym (zasłonięty splashem z ZIndex=9999).
    /// Przechodzimy przez każdą zakładkę FAKTYCZNIE ustawiając DashboardCurrentView, co powoduje
    /// że TransitioningContentControl podmienia content i PODŁĄCZA widok do drzewa. Avalonia
    /// wykonuje pełną inicjalizację. Przy następnej wizycie — zero kosztu, bo wszystko jest gotowe.
    /// </remarks>
    private async System.Threading.Tasks.Task InitializeAppAsync()
    {
        try
        {
            // Lista zakładek do rozgrzania — FAKTYCZNĄ nawigacją (podłączenie do visual tree)
            var tabSteps = new (string Status, Action Navigate)[]
            {
                ("Ładowanie rejestru postaci...", () => DashboardCurrentView = App.Current!.Services!.GetRequiredService<CharactersTabViewModel>()),
                ("Ładowanie rejestru przedmiotów...", () => DashboardCurrentView = App.Current!.Services!.GetRequiredService<ItemsTabViewModel>()),
                ("Ładowanie rejestru przeciwników...", () => DashboardCurrentView = App.Current!.Services!.GetRequiredService<AdversariesTabViewModel>()),
                ("Ładowanie ustawień...", () => DashboardCurrentView = App.Current!.Services!.GetRequiredService<SettingsTabViewModel>()),
                ("Ładowanie modułów...", () => DashboardCurrentView = App.Current!.Services!.GetRequiredService<OthersTabViewModel>()),
            };

            int totalSteps = tabSteps.Length;

            for (int i = 0; i < tabSteps.Length; i++)
            {
                LoadingStatus = tabSteps[i].Status;
                LoadingProgress = (double)i / totalSteps * 100.0;

                // Ustawiamy DashboardCurrentView → TransitioningContentControl
                // podłącza widok do drzewa wizualnego (pełna inicjalizacja)
                tabSteps[i].Navigate();

                // Oddajemy klatki renderowania — Avalonia potrzebuje minimum jednej
                // na template application i layout pass po podłączeniu do visual tree.
                await System.Threading.Tasks.Task.Delay(30);
            }

            // ── KROK 2: Transienty — tylko Build (wymuszenie kompilacji JIT), bez cache ──
            var transientSteps = new (string Status, string TypeName)[]
            {
                ("Przygotowywanie widoku kampanii...", "DungeonApp.Views.CampaignDetailView"),
                ("Przygotowywanie panelu głównego...", "DungeonApp.Views.Campaigns.Tabs.CampaignDashboardView"),
                ("Przygotowywanie trackera walki...", "DungeonApp.Views.Campaigns.Tabs.CampaignTrackerView"),
                ("Przygotowywanie sesji...", "DungeonApp.Views.Sessions.SessionDetailView"),
            };

            foreach (var (status, typeName) in transientSteps)
            {
                LoadingStatus = status;
                // Pasek postępu utrzymujemy na 90-99% dla transientów
                LoadingProgress = 90.0 + (new Random().NextDouble() * 9.0);
                await System.Threading.Tasks.Task.Delay(1);

                await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
                {
                    var viewType = Type.GetType(typeName);
                    if (viewType != null) Activator.CreateInstance(viewType);
                }, Avalonia.Threading.DispatcherPriority.Send);
            }

            LoadingProgress = 100;
            LoadingStatus = "Gotowe!";
            await System.Threading.Tasks.Task.Delay(100);

            // Ustawiamy docelową zakładkę i odsłaniamy interfejs
            ShowCampaigns();
            IsAppReady = true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[Init] Błąd inicjalizacji: {ex.Message}");
            ShowCampaigns();
            IsAppReady = true;
        }
    }

    private void UpdateTransition(NavigationIntent intent)
    {
        CurrentPageTransition = intent switch
        {
            NavigationIntent.DrillDown => new FluentEntranceTransition(TimeSpan.FromMilliseconds(200), true),
            NavigationIntent.DrillUp => new FluentEntranceTransition(TimeSpan.FromMilliseconds(200), false),
            _ => InstantTransition.Instance
        };
    }

    [RelayCommand]
    private void ToggleSidebar() => IsSidebarExpanded = !IsSidebarExpanded;

    [RelayCommand]
    private void ShowCampaigns() 
    { 
        UpdateTransition(NavigationIntent.Parallel);
        NavigationService.ClearNavigation(); 
        DashboardCurrentView = App.Current!.Services!.GetRequiredService<CampaignsTabViewModel>(); 
    }

    [RelayCommand]
    private void SelectRegistryTab(RegistryTab tab) 
    { 
        UpdateTransition(NavigationIntent.Parallel);
        NavigationService.ClearNavigation(); 
        DashboardCurrentView = tab switch
        {
            RegistryTab.Characters => App.Current!.Services!.GetRequiredService<CharactersTabViewModel>(),
            RegistryTab.Items => App.Current!.Services!.GetRequiredService<ItemsTabViewModel>(),
            RegistryTab.Adversaries => App.Current!.Services!.GetRequiredService<AdversariesTabViewModel>(),
            _ => App.Current!.Services!.GetRequiredService<CharactersTabViewModel>()
        };
    }

    [RelayCommand]
    private void ShowSettings() 
    { 
        UpdateTransition(NavigationIntent.Parallel);
        NavigationService.ClearNavigation(); 
        DashboardCurrentView = App.Current!.Services!.GetRequiredService<SettingsTabViewModel>(); 
    }
}
