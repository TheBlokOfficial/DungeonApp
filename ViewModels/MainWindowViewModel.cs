using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.Input;
using DungeonApp.Models;
using DungeonApp.Services;

namespace DungeonApp.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    private ViewModelBase? _currentView;
    public ViewModelBase? CurrentView
    {
        get => _currentView;
        set => SetProperty(ref _currentView, value);
    }
    
    private readonly CampaignService _campaignService = new();

    public ObservableCollection<Campaign> Campaigns { get; } = [];

    public MainWindowViewModel()
    {
        LoadCampaignsFromDisk();
    }

    private void LoadCampaignsFromDisk()
    {
        var loaded = _campaignService.LoadAllCampaigns();
        foreach (var campaign in loaded)
        {
            Campaigns.Add(campaign);
        }
    }
    
    [RelayCommand]
    private void RefreshCampaigns()
    {
        Campaigns.Clear();
        LoadCampaignsFromDisk();
    }
    
    [RelayCommand]
    private void CreateNewCampaign()
    {
        CurrentView = new CreateCampaignViewModel(this);
    }
    
    [RelayCommand]
    private void DeleteCampaign(Campaign campaign)
    {
        _campaignService.DeleteCampaign(campaign.Id);
        Campaigns.Remove(campaign);
    }
}
