using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using DungeonApp.Core.Modules.Clock;
using DungeonApp.Core.Modules.Scheduler;
using DungeonApp.Desktop.Shell;
using DungeonApp.Desktop.ViewModels;

namespace DungeonApp.Desktop.Features.CampaignWorkspace.Panels.Scheduler;

/// <summary>One thing waiting to happen, as the GM reads it.</summary>
public sealed class ScheduledEventViewModel(string name, string dueAt, string remaining)
{
    public string Name { get; } = name;

    /// <summary>Where it falls in world time, counted from the start of the campaign.</summary>
    public string DueAt { get; } = dueAt;

    public string Remaining { get; } = remaining;
}

/// <summary>
/// What the world is waiting for. Adds to the calendar and shows what is still on it; the module
/// decides what a legal plan is and when something has arrived.
/// </summary>
public sealed class SchedulerPanelViewModel : ObservableObject, IDisposable
{
    private readonly CampaignSession _session;
    private readonly SchedulerModule _scheduler;
    private readonly ClockModule _clock;

    private string _name = string.Empty;
    private string _reason = string.Empty;
    private string? _message;

    public SchedulerPanelViewModel(CampaignSession session, SchedulerModule scheduler, ClockModule clock)
    {
        _session = session;
        _scheduler = scheduler;
        _clock = clock;

        Delays =
        [
            Delay("za 1 h", TimeSpan.FromHours(1)),
            Delay("za 8 h", TimeSpan.FromHours(8)),
            Delay("za 1 d", TimeSpan.FromDays(1)),
            Delay("za 7 d", TimeSpan.FromDays(7))
        ];

        // One subscription covers everything: scheduling and time passing both commit through the
        // session, so neither needs to be watched separately.
        _session.Committed += Refresh;

        Refresh();
    }

    public ObservableCollection<ScheduledEventViewModel> Pending { get; } = [];

    public IReadOnlyList<PanelActionViewModel> Delays { get; }

    public bool IsEmpty => Pending.Count == 0;

    public string Name
    {
        get => _name;
        set
        {
            if (SetField(ref _name, value))
            {
                RaiseCanSchedule();
            }
        }
    }

    public string Reason
    {
        get => _reason;
        set
        {
            if (SetField(ref _reason, value))
            {
                RaiseCanSchedule();
            }
        }
    }

    public bool CanSchedule => !string.IsNullOrWhiteSpace(Name) && !string.IsNullOrWhiteSpace(Reason);

    public string? Message
    {
        get => _message;
        private set => SetField(ref _message, value);
    }

    public void Dispose() => _session.Committed -= Refresh;

    private PanelActionViewModel Delay(string label, TimeSpan delay)
        => new(label, new AsyncCommand(() => ScheduleAsync(delay), () => CanSchedule));

    private async Task ScheduleAsync(TimeSpan delay)
    {
        var name = Name;
        var reason = Reason;

        Message = await _session.ExecuteAsync(() => _scheduler.Schedule(delay, name, reason));

        if (Message is null)
        {
            // Cleared only on success, so a refused plan keeps what the GM typed.
            Name = string.Empty;
            Reason = string.Empty;
        }
    }

    private void Refresh()
    {
        Pending.Clear();

        var now = _clock.Now.TotalSeconds;

        foreach (var scheduled in _scheduler.Pending.OrderBy(entry => entry.DueAtSeconds))
        {
            Pending.Add(new ScheduledEventViewModel(
                scheduled.Name,
                CampaignTime.Describe(TimeSpan.FromSeconds(scheduled.DueAtSeconds)),
                CampaignTime.Describe(TimeSpan.FromSeconds(scheduled.DueAtSeconds - now))));
        }

        RaisePropertyChanged(nameof(IsEmpty));
    }

    private void RaiseCanSchedule()
    {
        RaisePropertyChanged(nameof(CanSchedule));

        foreach (var command in Delays.Select(delay => delay.Command).OfType<AsyncCommand>())
        {
            command.RaiseCanExecuteChanged();
        }
    }
}
