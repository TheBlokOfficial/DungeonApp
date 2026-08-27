using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using DungeonApp.Core.Modules.Dice;
using DungeonApp.Desktop.Shell;
using DungeonApp.Desktop.ViewModels;

namespace DungeonApp.Desktop.Features.CampaignWorkspace.Panels.Dice;

/// <summary>
/// The dice on the table. It asks for a roll and shows what fell; what a legal roll is, what fell
/// and how the result reads are all the module's, including the wording of a refusal.
/// </summary>
public sealed class DicePanelViewModel : ObservableObject
{
    private readonly CampaignSession _session;
    private readonly DiceModule _dice;

    private string _notation = "k20";
    private string _reason = string.Empty;
    private string? _message;
    private DiceRoll? _last;

    public DicePanelViewModel(CampaignSession session, DiceModule dice)
    {
        _session = session;
        _dice = dice;

        // The rolls a GM makes without thinking about them. Anything else goes in the box.
        Common =
        [
            Quick("k20"),
            Quick("k100"),
            Quick("2k6"),
            Quick("k6")
        ];

        RollCommand = new AsyncCommand(() => RollAsync(Notation), () => CanRoll);
    }

    public IReadOnlyList<PanelActionViewModel> Common { get; }

    public AsyncCommand RollCommand { get; }

    /// <summary>The result alone, big. The arithmetic behind it is one line below.</summary>
    public string Total => _last is null
        ? "—"
        : _last.Total.ToString(CultureInfo.CurrentCulture);

    /// <summary>Every die and the modifier, exactly as the chronicle recorded them.</summary>
    public string? Breakdown => _last is null ? null : DiceModule.Describe(_last);

    public bool HasRoll => _last is not null;

    public string Notation
    {
        get => _notation;
        set
        {
            if (SetField(ref _notation, value))
            {
                RaiseCanRoll();
            }
        }
    }

    /// <summary>
    /// What the roll decides. Required, the same as a reason for moving the world: a result in the
    /// chronicle with no question attached settles nothing anyone can read back.
    /// </summary>
    public string Reason
    {
        get => _reason;
        set
        {
            if (SetField(ref _reason, value))
            {
                RaiseCanRoll();
            }
        }
    }

    public bool CanRoll => !string.IsNullOrWhiteSpace(Reason);

    public string? Message
    {
        get => _message;
        private set => SetField(ref _message, value);
    }

    private PanelActionViewModel Quick(string notation)
        => new(notation, new AsyncCommand(() => RollAsync(notation), () => CanRoll));

    private async Task RollAsync(string notation)
    {
        var reason = Reason;
        DiceRoll? rolled = null;

        Message = await _session.ExecuteAsync(() => rolled = _dice.Roll(notation, reason));

        if (Message is not null)
        {
            // A refused roll keeps what the GM typed, and keeps the previous result on the table -
            // clearing it would look like the roll happened and came out blank.
            return;
        }

        _last = rolled;
        Reason = string.Empty;

        RaisePropertyChanged(nameof(Total));
        RaisePropertyChanged(nameof(Breakdown));
        RaisePropertyChanged(nameof(HasRoll));
    }

    private void RaiseCanRoll()
    {
        RaisePropertyChanged(nameof(CanRoll));
        RollCommand.RaiseCanExecuteChanged();

        foreach (var command in Common.Select(action => action.Command).OfType<AsyncCommand>())
        {
            command.RaiseCanExecuteChanged();
        }
    }
}
