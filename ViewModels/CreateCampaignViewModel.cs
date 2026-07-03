using System;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DungeonApp.Models;
using DungeonApp.Services;

namespace DungeonApp.ViewModels;

public partial class CreateCampaignViewModel : ViewModelBase
{
    private readonly MainWindowViewModel _mainViewModel;
    private readonly CampaignService _campaignService = new();

    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    private string _system = "D&D 5e";

    [ObservableProperty]
    private string? _errorMessage;

    public CreateCampaignViewModel(MainWindowViewModel mainViewModel)
    {
        _mainViewModel = mainViewModel;
    }

    [RelayCommand]
    private void Cancel()
    {
        _mainViewModel.CurrentView = null;
    }

    [RelayCommand]
    private void Create()
    {
        ErrorMessage = null;

        var resolvedName = string.IsNullOrWhiteSpace(Name)
            ? "Nowa Kampania " + DateTime.Now.ToString("yyyy.MM.dd")
            : Name.Trim();

        var newCampaign = new Campaign
        {
            Name = resolvedName,
            System = string.IsNullOrWhiteSpace(System) ? "D&D 5e" : System.Trim(),
            CreatedAt = DateTime.Now,
            LastSession = DateTime.Now,
            SessionsCount = 0
        };

        _campaignService.SaveCampaign(newCampaign);

        _mainViewModel.Campaigns.Insert(0, newCampaign);
        _mainViewModel.CurrentView = null;
    }
}