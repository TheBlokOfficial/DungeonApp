using System;
using System.Threading.Tasks;
using DungeonApp.Core.Content;
using DungeonApp.Desktop.Content;
using DungeonApp.Desktop.ViewModels;

namespace DungeonApp.Content.Dnd5e;

/// <summary>
/// One row of the campaign's instance list: the name to show and, when the instance did not resolve,
/// the Polish sentence explaining what is wrong. An unresolved instance is never left off this list -
/// it gets a row and a message instead, mirroring how the registry marks a broken entry rather than
/// hiding it (docs/architecture.md, "Co się dzieje, gdy treść jest zepsuta").
/// <para>
/// A resolved monster row also carries its current hit points, changed only through
/// <see cref="SaveHitPointsCommand"/> - never through the <see cref="CurrentHp"/> setter itself, so a
/// write is always something a caller can await instead of one that runs unobserved.
/// </para>
/// </summary>
public sealed class InstanceRowViewModel : ObservableObject, IDisposable
{
    private readonly CampaignToolContext _context;
    private readonly InstanceId _id;
    private readonly ResolvedInstance _resolved;

    private int? _currentHp;
    private bool _isDisposed;

    public InstanceRowViewModel(
        CampaignToolContext context,
        InstanceId id,
        string displayName,
        string? message,
        ResolvedInstance resolved,
        ContentId ownerSet)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(resolved);

        _context = context;
        _id = id;
        _resolved = resolved;

        DisplayName = displayName;
        Message = message;

        // Branching on a content type's own id ("monster") is legal here and nowhere outside this
        // content set - see docs/decisions.md on where that knowledge is allowed to live.
        CanEditHitPoints = resolved.Unresolved is null
            && resolved.Source is { } source
            && source.Entry.Type.Set == ownerSet
            && source.Entry.Type.Type.Value == Dnd5eContentSet.MonsterTypeId;

        if (CanEditHitPoints)
        {
            var monster = resolved.Values!.Read<Monster>();
            _currentHp = monster.CurrentHp ?? monster.Hp;
            MaxHp = monster.Hp;
        }

        SaveHitPointsCommand = new AsyncCommand(SaveHitPointsAsync, () => CanEditHitPoints && !_isDisposed);
        RemoveCommand = new AsyncCommand(RemoveAsync, () => !_isDisposed);
    }

    public string DisplayName { get; }

    public string? Message { get; }

    public bool HasMessage => Message is not null;

    /// <summary>True only for a resolved instance of this set's own monster content type.</summary>
    public bool CanEditHitPoints { get; }

    /// <summary>The entry's own maximum, shown next to the editable field - never itself editable here.</summary>
    public int MaxHp { get; }

    /// <summary>
    /// The field the numeric input is bound to. A plain store, the same way
    /// <see cref="CampaignInstancesToolViewModel.SelectedToAdd"/> is: typing here saves nothing by
    /// itself, only <see cref="SaveHitPointsCommand"/> does.
    /// </summary>
    public int? CurrentHp
    {
        get => _currentHp;
        set => SetField(ref _currentHp, value);
    }

    public AsyncCommand SaveHitPointsCommand { get; }

    public AsyncCommand RemoveCommand { get; }

    public void Dispose()
    {
        if (_isDisposed)
        {
            return;
        }

        _isDisposed = true;
        SaveHitPointsCommand.RaiseCanExecuteChanged();
        RemoveCommand.RaiseCanExecuteChanged();
    }

    private async Task SaveHitPointsAsync()
    {
        // Diffed against the entry's own values, never the merged ones - differencing against the
        // merged record would compare it to itself and always produce an empty patch, dropping
        // whatever the instance already deviated on.
        var entryValues = _resolved.Source!.Entry.Values;
        var monster = _resolved.Values!.Read<Monster>();
        var candidate = ContentValues.From(monster with { CurrentHp = CurrentHp });
        var patch = ContentValues.Difference(baseline: entryValues, candidate: candidate);

        await _context.ExecuteAsync(() => _context.Instances.ReplacePatch(_id, patch));
    }

    private async Task RemoveAsync() =>
        await _context.ExecuteAsync(() => _context.Instances.Remove(_id));
}
