using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DungeonApp.Models;
using DungeonApp.Services;
using Microsoft.Extensions.DependencyInjection;

namespace DungeonApp.ViewModels.Dashboard;

public partial class CampaignsTabViewModel : ViewModelBase
{
    private readonly ICampaignService _campaignService;
    public INavigationService NavigationService { get; }

    public ObservableCollection<Campaign> Campaigns { get; } = new();

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private bool _isDataLoaded;

    /// <summary>
    /// Buforowane instancje widoków listy i gridu kampanii.
    /// </summary>
    /// <remarks>
    /// DLACZEGO: Wcześniej ActiveViewContent był computed property, który tworzył nową instancję VM
    /// przy KAŻDYM odczycie (każde PropertyChanged → binding → nowy VM → cache miss w ViewLocator →
    /// pełna przebudowa widoku XAML). To była główna przyczyna stutterów w panelu kampanii.
    /// Buforowanie instancji gwarantuje, że ViewLocator zwróci widok z cache (0ms).
    /// </remarks>
    private CampaignsListViewModel? _cachedListView;
    private CampaignsGridViewModel? _cachedGridView;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ActiveViewContent))]
    [NotifyPropertyChangedFor(nameof(IsCampaignsGridView))]
    private bool _isCampaignsListView = true;

    public bool IsCampaignsGridView => !IsCampaignsListView;

    public ViewModelBase ActiveViewContent
    {
        get
        {
            if (IsCampaignsListView)
                return _cachedListView ??= new CampaignsListViewModel(this);
            else
                return _cachedGridView ??= new CampaignsGridViewModel(this);
        }
    }

    [RelayCommand]
    private void SetCampaignsViewMode(string mode)
    {
        IsCampaignsListView = mode == "List";
    }

    public CampaignsTabViewModel(ICampaignService campaignService, INavigationService navigationService)
    {
        _campaignService = campaignService;
        NavigationService = navigationService;
    }

    /// <summary>
    /// Cykl życia nawigacji — wywoływane za każdym razem, gdy użytkownik przechodzi na tę zakładkę.
    /// </summary>
    /// <remarks>
    /// Za pierwszym razem ładuje dane z dysku. Przy kolejnych wejściach (np. powrót z detali kampanii)
    /// odświeża listę, bo użytkownik mógł zmienić dane w kampanii (dodać/usunąć postać, edytować nazwę).
    /// </remarks>
    public override void OnNavigatedTo()
    {
        _ = ReloadDataAsync();
    }

    private async Task ReloadDataAsync()
    {
        try
        {
            IsLoading = true;

            var campaigns = await Task.Run(() => _campaignService.LoadAllCampaigns());

            Campaigns.Clear();
            foreach (var campaign in campaigns)
            {
                Campaigns.Add(campaign);
            }

            IsDataLoaded = true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[CampaignsTab] Błąd ładowania kampanii: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private void RefreshCampaigns() => _ = ReloadDataAsync();

    [RelayCommand]
    private void CreateNewCampaign()
    {
        var vm = App.Current!.Services!.GetRequiredService<CreateCampaignViewModel>();
        NavigationService.ShowOverlay(vm);
    }

    [RelayCommand]
    private void DeleteCampaign(Campaign campaign)
    {
        _campaignService.DeleteCampaign(campaign.Id);
        Campaigns.Remove(campaign);
    }

    [RelayCommand]
    private void StartCampaign(Campaign campaign)
    {
        var vm = ActivatorUtilities.CreateInstance<CampaignDetailViewModel>(App.Current!.Services!, campaign);
        NavigationService.NavigateTo(vm);
    }
    
    [RelayCommand]
    private void OpenCampaign(Campaign campaign)
    {
        StartCampaign(campaign);
    }
}

public class CampaignsListViewModel : ViewModelBase
{
    public CampaignsTabViewModel Parent { get; }
    public CampaignsListViewModel(CampaignsTabViewModel parent) { Parent = parent; }
}

public class CampaignsGridViewModel : ViewModelBase
{
    public CampaignsTabViewModel Parent { get; }
    public CampaignsGridViewModel(CampaignsTabViewModel parent) { Parent = parent; }
}
