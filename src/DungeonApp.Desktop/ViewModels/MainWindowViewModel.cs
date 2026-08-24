using System;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using DungeonApp.Application.Campaigns;

namespace DungeonApp.Desktop.ViewModels;

public sealed class MainWindowViewModel : INotifyPropertyChanged
{
    private readonly CreateCampaignUseCase _createCampaign;
    private readonly AdvanceCampaignTimeUseCase _advanceCampaignTime;
    private readonly GetCampaignSessionUseCase _getCampaignSession;
    private readonly ListCampaignsUseCase _listCampaigns;
    private readonly EnableCampaignModuleUseCase _enableCampaignModule;
    private readonly ScheduleWorldEventUseCase _scheduleWorldEvent;
    private CampaignSession? _activeCampaign;
    private CampaignListItemViewModel? _selectedCampaign;
    private string _newCampaignName = "Nowa kampania";
    private bool _isClockEnabled = true;
    private string _advanceMinutes = "30";
    private string _advanceReason = "";
    private string _statusMessage = "Utwórz kampanię, aby rozpocząć sesję.";
    private string _scheduledEventDelayMinutes = "60";
    private string _scheduledEventTitle = "";
    private string _scheduledEventReason = "";

    public MainWindowViewModel(
        CreateCampaignUseCase createCampaign,
        AdvanceCampaignTimeUseCase advanceCampaignTime,
        GetCampaignSessionUseCase getCampaignSession,
        ListCampaignsUseCase listCampaigns,
        EnableCampaignModuleUseCase enableCampaignModule,
        ScheduleWorldEventUseCase scheduleWorldEvent)
    {
        _createCampaign = createCampaign;
        _advanceCampaignTime = advanceCampaignTime;
        _getCampaignSession = getCampaignSession;
        _listCampaigns = listCampaigns;
        _enableCampaignModule = enableCampaignModule;
        _scheduleWorldEvent = scheduleWorldEvent;

        CreateCampaignCommand = new AsyncCommand(CreateCampaignAsync);
        AdvanceTimeCommand = new AsyncCommand(AdvanceTimeAsync, () => CanAdvanceTime);
        LoadCampaignsCommand = new AsyncCommand(LoadCampaignsAsync);
        OpenCampaignCommand = new AsyncCommand(OpenCampaignAsync, () => CanOpenCampaign);
        EnableClockCommand = new AsyncCommand(EnableClockAsync, () => CanEnableClock);
        EnableSchedulerCommand = new AsyncCommand(EnableSchedulerAsync, () => CanEnableScheduler);
        ScheduleWorldEventCommand = new AsyncCommand(ScheduleWorldEventAsync, () => CanScheduleWorldEvent);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public ICommand CreateCampaignCommand { get; }

    public AsyncCommand AdvanceTimeCommand { get; }

    public AsyncCommand LoadCampaignsCommand { get; }

    public AsyncCommand OpenCampaignCommand { get; }

    public AsyncCommand EnableClockCommand { get; }

    public AsyncCommand EnableSchedulerCommand { get; }

    public AsyncCommand ScheduleWorldEventCommand { get; }

    public ObservableCollection<HistoryItemViewModel> History { get; } = [];

    public ObservableCollection<CampaignListItemViewModel> Campaigns { get; } = [];

    public ObservableCollection<ScheduledWorldEventItemViewModel> ScheduledWorldEvents { get; } = [];

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

    public string AdvanceMinutes
    {
        get => _advanceMinutes;
        set => SetField(ref _advanceMinutes, value);
    }

    public string AdvanceReason
    {
        get => _advanceReason;
        set => SetField(ref _advanceReason, value);
    }

    public string StatusMessage
    {
        get => _statusMessage;
        private set => SetField(ref _statusMessage, value);
    }

    public string ScheduledEventDelayMinutes
    {
        get => _scheduledEventDelayMinutes;
        set => SetField(ref _scheduledEventDelayMinutes, value);
    }

    public string ScheduledEventTitle
    {
        get => _scheduledEventTitle;
        set => SetField(ref _scheduledEventTitle, value);
    }

    public string ScheduledEventReason
    {
        get => _scheduledEventReason;
        set => SetField(ref _scheduledEventReason, value);
    }

    public bool HasActiveCampaign => _activeCampaign is not null;

    public bool ShowCreateCampaign => !HasActiveCampaign;

    public bool CanAdvanceTime => _activeCampaign?.WorldTime is not null;

    public bool CanOpenCampaign => SelectedCampaign is not null;

    public bool CanEnableClock => _activeCampaign is not null && !_activeCampaign.EnabledModuleIds.Contains("core.clock");

    public bool CanEnableScheduler =>
        _activeCampaign is not null &&
        _activeCampaign.EnabledModuleIds.Contains("core.clock") &&
        !_activeCampaign.EnabledModuleIds.Contains("core.scheduler");

    public bool CanScheduleWorldEvent => _activeCampaign?.EnabledModuleIds.Contains("core.scheduler") is true;

    public string CampaignName => _activeCampaign?.Name ?? "No active campaign";

    public string CurrentTime => _activeCampaign?.WorldTime is { } time
        ? $"{time.Days}d {time.Hours}h {time.Minutes}m"
        : "Moduł zegara jest wyłączony";

    private async Task CreateCampaignAsync()
    {
        try
        {
            ApplySession(await _createCampaign.ExecuteAsync(new CreateCampaignRequest(NewCampaignName, IsClockEnabled)));
            await LoadCampaignsAsync();
            SelectedCampaign = Campaigns.SingleOrDefault(item => item.Id == _activeCampaign!.Id);
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
            var summaries = await _listCampaigns.ExecuteAsync();
            Campaigns.Clear();
            foreach (var summary in summaries)
            {
                Campaigns.Add(new CampaignListItemViewModel(summary));
            }

            StatusMessage = Campaigns.Count == 0
                ? "Brak zapisanych kampanii."
                : $"Wczytano kampanie: {Campaigns.Count}.";
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
            ApplySession(await _getCampaignSession.ExecuteAsync(SelectedCampaign.Id));
            StatusMessage = "Kampania została otwarta.";
        }
        catch (Exception exception)
        {
            StatusMessage = exception.Message;
        }
    }

    private async Task AdvanceTimeAsync()
    {
        if (_activeCampaign is null || !int.TryParse(AdvanceMinutes, out var minutes) || minutes <= 0)
        {
            StatusMessage = "Podaj dodatnią liczbę minut.";
            return;
        }

        if (string.IsNullOrWhiteSpace(AdvanceReason))
        {
            StatusMessage = "Opisz powód upływu czasu świata.";
            return;
        }

        try
        {
            await _advanceCampaignTime.ExecuteAsync(
                new AdvanceCampaignTimeRequest(_activeCampaign.Id, TimeSpan.FromMinutes(minutes), AdvanceReason));
            ApplySession(await _getCampaignSession.ExecuteAsync(_activeCampaign.Id));
            AdvanceReason = "";
            StatusMessage = "Czas świata przesunięto, a kampanię zapisano.";
        }
        catch (Exception exception)
        {
            StatusMessage = exception.Message;
        }
    }

    private async Task EnableClockAsync()
    {
        if (_activeCampaign is null)
        {
            return;
        }

        try
        {
            ApplySession(await _enableCampaignModule.ExecuteAsync(
                new EnableCampaignModuleRequest(
                    _activeCampaign.Id,
                    "core.clock",
                    "Włączono śledzenie czasu świata.")));
            await LoadCampaignsAsync();
            StatusMessage = "Moduł zegara został włączony i zapisany w historii kampanii.";
        }
        catch (Exception exception)
        {
            StatusMessage = exception.Message;
        }
    }

    private async Task EnableSchedulerAsync()
    {
        if (_activeCampaign is null)
        {
            return;
        }

        try
        {
            ApplySession(await _enableCampaignModule.ExecuteAsync(
                new EnableCampaignModuleRequest(
                    _activeCampaign.Id,
                    "core.scheduler",
                    "Włączono harmonogram zdarzeń świata.")));
            await LoadCampaignsAsync();
            StatusMessage = "Harmonogram został włączony i zapisany w historii kampanii.";
        }
        catch (Exception exception)
        {
            StatusMessage = exception.Message;
        }
    }

    private async Task ScheduleWorldEventAsync()
    {
        if (_activeCampaign is null ||
            !int.TryParse(ScheduledEventDelayMinutes, out var minutes) ||
            minutes <= 0)
        {
            StatusMessage = "Podaj dodatnią liczbę minut do terminu zdarzenia.";
            return;
        }

        if (string.IsNullOrWhiteSpace(ScheduledEventTitle) || string.IsNullOrWhiteSpace(ScheduledEventReason))
        {
            StatusMessage = "Podaj nazwę i powód planowanego zdarzenia.";
            return;
        }

        try
        {
            ApplySession(await _scheduleWorldEvent.ExecuteAsync(
                new ScheduleWorldEventRequest(
                    _activeCampaign.Id,
                    TimeSpan.FromMinutes(minutes),
                    ScheduledEventTitle,
                    ScheduledEventReason)));
            ScheduledEventTitle = "";
            ScheduledEventReason = "";
            StatusMessage = "Zdarzenie świata zostało zaplanowane.";
        }
        catch (Exception exception)
        {
            StatusMessage = exception.Message;
        }
    }

    private void ApplySession(CampaignSession session)
    {
        _activeCampaign = session;
        History.Clear();
        foreach (var entry in session.History.Reverse())
        {
            History.Add(new HistoryItemViewModel(entry));
        }

        ScheduledWorldEvents.Clear();
        foreach (var scheduledEvent in session.ScheduledWorldEvents)
        {
            ScheduledWorldEvents.Add(new ScheduledWorldEventItemViewModel(scheduledEvent));
        }

        RaisePropertyChanged(nameof(HasActiveCampaign));
        RaisePropertyChanged(nameof(ShowCreateCampaign));
        RaisePropertyChanged(nameof(CanAdvanceTime));
        RaisePropertyChanged(nameof(CanEnableClock));
        RaisePropertyChanged(nameof(CanEnableScheduler));
        RaisePropertyChanged(nameof(CanScheduleWorldEvent));
        RaisePropertyChanged(nameof(CampaignName));
        RaisePropertyChanged(nameof(CurrentTime));
        AdvanceTimeCommand.RaiseCanExecuteChanged();
        EnableClockCommand.RaiseCanExecuteChanged();
        EnableSchedulerCommand.RaiseCanExecuteChanged();
        ScheduleWorldEventCommand.RaiseCanExecuteChanged();
    }

    private void RaisePropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

    private bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return false;
        }

        field = value;
        RaisePropertyChanged(propertyName);
        return true;
    }
}
