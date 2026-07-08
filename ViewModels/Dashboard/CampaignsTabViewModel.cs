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
    [NotifyPropertyChangedFor(nameof(ActiveViewContent))]
    private bool _isCampaignsListView = true;

    public bool IsCampaignsGridView => !IsCampaignsListView;

    public ViewModelBase ActiveViewContent => IsCampaignsListView 
        ? new CampaignsListViewModel(this) 
        : new CampaignsGridViewModel(this);

    [RelayCommand]
    private void SetCampaignsViewMode(string mode)
    {
        IsCampaignsListView = mode == "List";
        OnPropertyChanged(nameof(IsCampaignsGridView));
    }

    public CampaignsTabViewModel(ICampaignService campaignService, INavigationService navigationService)
    {
        _campaignService = campaignService;
        NavigationService = navigationService;
        LoadCampaignsFromDisk();
    }

    private void LoadCampaignsFromDisk()
    {
        Campaigns.Clear();
        foreach (var campaign in _campaignService.LoadAllCampaigns())
        {
            Campaigns.Add(campaign);
        }
    }

    [RelayCommand]
    private void RefreshCampaigns() => LoadCampaignsFromDisk();

    [RelayCommand]
    private void CreateNewCampaign()
    {
        var vm = App.Current!.Services!.GetRequiredService<CreateCampaignViewModel>();
        NavigationService.NavigateTo(vm);
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
