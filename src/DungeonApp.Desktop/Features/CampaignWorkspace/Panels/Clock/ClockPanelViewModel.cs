using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using DungeonApp.Core.Modules.Clock;
using DungeonApp.Desktop.Shell;
using DungeonApp.Desktop.ViewModels;

namespace DungeonApp.Desktop.Features.CampaignWorkspace.Panels.Clock;

/// <summary>
/// The world clock on the desk. It reads the module and asks it to move; it does not decide what a
/// legal move is, and it does not phrase the refusal - both belong to the module.
/// </summary>
public sealed class ClockPanelViewModel : ObservableObject
{
    private readonly CampaignSession _session;
    private readonly ClockModule _clock;

    private string _reason = string.Empty;
    private string? _message;

    public ClockPanelViewModel(CampaignSession session, ClockModule clock)
    {
        _session = session;
        _clock = clock;

        Steps =
        [
            Step("+10 min", TimeSpan.FromMinutes(10)),
            Step("+1 h", TimeSpan.FromHours(1)),
            Step("+8 h", TimeSpan.FromHours(8)),
            Step("+1 d", TimeSpan.FromDays(1))
        ];
    }

    public IReadOnlyList<PanelActionViewModel> Steps { get; }

    /// <summary>Elapsed since the campaign began. Not a date: the core keeps no calendar.</summary>
    public string Elapsed => CampaignTime.Describe(_clock.Now.Elapsed);

    /// <summary>
    /// Why the world is moving. The module refuses a move without one, so this is a required field
    /// rather than a nicety - it is what the chronicle will explain the change with.
    /// </summary>
    public string Reason
    {
        get => _reason;
        set
        {
            if (SetField(ref _reason, value))
            {
                RaisePropertyChanged(nameof(CanAdvance));
                RaiseStepStates();
            }
        }
    }

    public bool CanAdvance => !string.IsNullOrWhiteSpace(Reason);

    /// <summary>The last refusal or warning. Null when there is nothing to say.</summary>
    public string? Message
    {
        get => _message;
        private set => SetField(ref _message, value);
    }

    private PanelActionViewModel Step(string label, TimeSpan amount)
        => new(label, new AsyncCommand(() => AdvanceAsync(amount), () => CanAdvance));

    private async Task AdvanceAsync(TimeSpan amount)
    {
        var reason = Reason;

        Message = await _session.ExecuteAsync(() => _clock.Advance(amount, reason));

        if (Message is null)
        {
            // Cleared only on success, so a refused move keeps what the GM typed.
            Reason = string.Empty;
        }

        RaisePropertyChanged(nameof(Elapsed));
    }

    private void RaiseStepStates()
    {
        foreach (var command in Steps.Select(step => step.Command).OfType<AsyncCommand>())
        {
            command.RaiseCanExecuteChanged();
        }
    }
}
