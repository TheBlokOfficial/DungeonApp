using System;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DungeonApp.Models;
using DungeonApp.Services;

namespace DungeonApp.ViewModels;

public partial class CreateCampaignViewModel : ViewModelBase
{
    private readonly INavigationService _navigationService;
    private readonly ICampaignService _campaignService;

    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    private string _system = "D&D 5e";

    [ObservableProperty]
    private string? _errorMessage;

    public CreateCampaignViewModel(
        INavigationService navigationService,
        ICampaignService campaignService)
    {
        _navigationService = navigationService;
        _campaignService = campaignService;
    }

    [RelayCommand]
    private void Cancel()
    {
        _navigationService.NavigateBack();
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

        // Odświeżenie w MainWindowViewModel zostanie zrobione po powrocie jeśli trzeba.
        // Bądźmy prości: po powrocie można by wrzucić zdarzenie, ale by MainWindow miało nową kampanię,
        // musielibyśmy zawiadomić MainWindow. For now, we will NavigateBack().
        _navigationService.NavigateBack();
        
        // Zaktualizujmy by wymusić odświeżenie:
        if (App.Current?.Services?.GetService(typeof(DungeonApp.ViewModels.Dashboard.CampaignsTabViewModel)) is DungeonApp.ViewModels.Dashboard.CampaignsTabViewModel campaignsVm)
        {
            campaignsVm.Campaigns.Insert(0, newCampaign);
        }
    }
}
