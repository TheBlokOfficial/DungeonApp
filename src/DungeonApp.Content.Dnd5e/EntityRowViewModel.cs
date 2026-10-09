using System;
using System.Threading.Tasks;
using DungeonApp.Core.Entries;
using DungeonApp.Core.Entries.Entities;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DungeonApp.Desktop.Entries;

namespace DungeonApp.Content.Dnd5e;

/// <summary>
/// One row of the campaign's entity list: the name to show and, when the entity did not resolve,
/// the Polish sentence explaining what is wrong. An unresolved entity is never left off this list -
/// it gets a row and a message instead, mirroring how a content tab marks a broken entry rather
/// than hiding it.
/// <para>
/// A resolved row of a creature also carries its current hit points, changed only through
/// <see cref="SaveHitPointsCommand"/> - never through the <see cref="CurrentHp"/> setter itself, so a
/// write is always something a caller can await instead of one that runs unobserved.
/// </para>
/// </summary>
public sealed partial class EntityRowViewModel : ObservableObject, IDisposable
{
    private readonly CampaignEntriesContext _context;
    private readonly CampaignEntity _entity;
    private readonly ResolvedEntity _resolved;

    private bool _isDisposed;

    public EntityRowViewModel(
        CampaignEntriesContext context,
        string displayName,
        string? message,
        ResolvedEntity resolved,
        ContentId ownerSet)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(resolved);

        _context = context;
        _entity = resolved.Entity;
        _resolved = resolved;

        DisplayName = displayName;
        Message = message;

        // Branching on a content type's own id ("creature") is legal here and nowhere outside this
        // system: knowledge of D&D content types lives only in this project.
        if (resolved.Unresolved is null
            && resolved.Source is { } source
            && source.Entry.Type.Set == ownerSet
            && source.Entry.Type.Type.Value == Dnd5eSystem.CreatureTypeId)
        {
            var combat = resolved.Values!.Read<Creature>().Combat;
            CanEditHitPoints = true;
            CurrentHp = combat.CurrentHp ?? combat.Hp;
            MaxHp = combat.Hp;
        }
    }

    public string DisplayName { get; }

    public string? Message { get; }

    public bool HasMessage => Message is not null;

    /// <summary>True only for a resolved entity of this set's creature type.</summary>
    public bool CanEditHitPoints { get; }

    /// <summary>The entry's own maximum, shown next to the editable field - never itself editable here.</summary>
    public int MaxHp { get; }

    /// <summary>
    /// The field the numeric input is bound to. A plain store, the same way
    /// <see cref="CampaignEntitiesToolViewModel.SelectedToAdd"/> is: typing here saves nothing by
    /// itself, only <see cref="SaveHitPointsCommand"/> does.
    /// </summary>
    [ObservableProperty]
    public partial int? CurrentHp { get; set; }

    public void Dispose()
    {
        if (_isDisposed)
        {
            return;
        }

        _isDisposed = true;
        SaveHitPointsCommand.NotifyCanExecuteChanged();
        RemoveCommand.NotifyCanExecuteChanged();
    }

    [RelayCommand(CanExecute = nameof(CanSaveHitPoints))]
    private async Task SaveHitPointsAsync()
    {
        // Diffed against the entry's own values, never the merged ones - differencing against the
        // merged record would compare it to itself and always produce an empty patch, dropping
        // whatever the entity already deviated on.
        var entryValues = _resolved.Source!.Entry.Values;
        var creature = _resolved.Values!.Read<Creature>();
        var candidate = ContentValues.From(creature with { Combat = creature.Combat with { CurrentHp = CurrentHp } });
        var patch = ContentValues.Difference(baseline: entryValues, candidate: candidate);

        await _context.ChangeAsync(CampaignEntityChanges.ReplacePatch(_entity, patch));
    }

    private bool CanSaveHitPoints() => CanEditHitPoints && !_isDisposed;

    [RelayCommand(CanExecute = nameof(CanRemove))]
    private async Task RemoveAsync() =>
        await _context.ChangeAsync(CampaignEntityChanges.Remove(_entity.Id));

    private bool CanRemove() => !_isDisposed;
}
