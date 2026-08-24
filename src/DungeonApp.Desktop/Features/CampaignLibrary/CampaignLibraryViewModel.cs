using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using DungeonApp.Application.Campaigns;
using DungeonApp.Desktop.ViewModels;

namespace DungeonApp.Desktop.Features.CampaignLibrary;

public sealed class CampaignLibraryViewModel : ObservableObject
{
    private readonly CreateCampaignUseCase _createCampaign;
    private readonly GetCampaignSessionUseCase _getCampaignSession;
    private readonly ListCampaignsUseCase _listCampaigns;
    private CampaignListItemViewModel? _selectedCampaign;
    private string _newCampaignName = "Nowa kampania";
    private bool _isClockEnabled = true;
    private string _statusMessage = "Wczytywanie biblioteki kampanii…";

    public CampaignLibraryViewModel(
        CreateCampaignUseCase createCampaign,
        GetCampaignSessionUseCase getCampaignSession,
        ListCampaignsUseCase listCampaigns)
    {
        _createCampaign = createCampaign;
        _getCampaignSession = getCampaignSession;
        _listCampaigns = listCampaigns;

        CreateCampaignCommand = new AsyncCommand(CreateCampaignAsync);
        LoadCampaignsCommand = new AsyncCommand(LoadCampaignsAsync);
        OpenCampaignCommand = new AsyncCommand(OpenCampaignAsync, () => CanOpenCampaign);
    }

    public ObservableCollection<CampaignListItemViewModel> Campaigns { get; } = [];

    public AsyncCommand CreateCampaignCommand { get; }

    public AsyncCommand LoadCampaignsCommand { get; }

    public AsyncCommand OpenCampaignCommand { get; }

    public CampaignListItemViewModel? SelectedCampaign
    {
        get => _selectedCampaign;
        set
        {
            if (SetField(ref _selectedCampaign, value))
            {
                RaisePropertyChanged(nameof(CanOpenCampaign));
                OpenCampaignCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public string NewCampaignName
    {
        get => _newCampaignName;
        set => SetField(ref _newCampaignName, value);
    }

    public bool IsClockEnabled
    {
        get => _isClockEnabled;
        set => SetField(ref _isClockEnabled, value);
    }

    public string StatusMessage
    {
        get => _statusMessage;
        private set => SetField(ref _statusMessage, value);
    }

    public bool HasCampaigns => Campaigns.Count > 0;

    public bool IsEmpty => !HasCampaigns;

    public bool CanOpenCampaign => SelectedCampaign is not null;

    private async Task CreateCampaignAsync()
    {
        if (string.IsNullOrWhiteSpace(NewCampaignName))
        {
            StatusMessage = "Nazwa kampanii nie może być pusta.";
            return;
        }

        try
        {
            var session = await _createCampaign.ExecuteAsync(new CreateCampaignRequest(NewCampaignName.Trim(), IsClockEnabled));
            await LoadCampaignsAsync();
            SelectedCampaign = Campaigns.SingleOrDefault(item => item.Id == session.Id);
            NewCampaignName = "Nowa kampania";
            StatusMessage = "Kampania została utworzona i zapisana lokalnie.";
        }
        catch (Exception exception)
        {
            StatusMessage = exception.Message;
        }
    }

    private async Task LoadCampaignsAsync()
    {
        try
        {
            var selectedId = SelectedCampaign?.Id;
            var summaries = await _listCampaigns.ExecuteAsync();

            Campaigns.Clear();
            foreach (var summary in summaries)
            {
                Campaigns.Add(new CampaignListItemViewModel(summary));
            }

            SelectedCampaign = selectedId is null
                ? Campaigns.FirstOrDefault()
                : Campaigns.SingleOrDefault(item => item.Id == selectedId) ?? Campaigns.FirstOrDefault();

            RaisePropertyChanged(nameof(HasCampaigns));
            RaisePropertyChanged(nameof(IsEmpty));
            StatusMessage = Campaigns.Count == 0
                ? "Brak zapisanych kampanii. Utwórz pierwszą, aby rozpocząć."
                : $"Biblioteka gotowa · {Campaigns.Count} kampanii.";
        }
        catch (Exception exception)
        {
            StatusMessage = exception.Message;
        }
    }

    private async Task OpenCampaignAsync()
    {
        if (SelectedCampaign is null)
        {
            return;
        }

        try
        {
            var session = await _getCampaignSession.ExecuteAsync(SelectedCampaign.Id);
            StatusMessage = $"Otwarto „{session.Name}”. Workspace kampanii powstanie w następnym przyroście.";
        }
        catch (Exception exception)
        {
            StatusMessage = exception.Message;
        }
    }
}
