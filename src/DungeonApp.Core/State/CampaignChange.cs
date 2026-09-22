using System;
using System.Collections.Generic;

namespace DungeonApp.Core.State;

/// <summary>
/// Everything one call through the campaign's single change entry point will do, built up before
/// that call ever happens: a new version of a record (an upsert - existing means changed,
/// unfamiliar means created) or a deletion by id, against a named model. One change can touch
/// several records and several models - the fourth ban's "kilka rzeczy naraz".
/// </summary>
public sealed class CampaignChange
{
    private readonly List<RecordEdit> _edits = [];

    /// <summary>Sets <paramref name="record"/> as <paramref name="model"/>'s current version of whatever it identifies - creating it if this is the first version <see cref="CampaignStateSnapshot.Apply"/> has seen for that id.</summary>
    public CampaignChange Upsert<TRecord>(StateModelDeclaration<TRecord> model, TRecord record)
        where TRecord : IStateRecord
    {
        ArgumentNullException.ThrowIfNull(model);
        ArgumentNullException.ThrowIfNull(record);

        _edits.Add(new RecordUpsert(model.ModelId, record));
        return this;
    }

    /// <summary>Removes the record named <paramref name="recordId"/> from <paramref name="model"/>.</summary>
    public CampaignChange Delete<TRecord>(StateModelDeclaration<TRecord> model, string recordId)
        where TRecord : IStateRecord
    {
        ArgumentNullException.ThrowIfNull(model);
        ArgumentException.ThrowIfNullOrWhiteSpace(recordId);

        _edits.Add(new RecordDeletion(model.ModelId, recordId));
        return this;
    }

    internal IReadOnlyList<RecordEdit> Edits => _edits;
}

internal abstract record RecordEdit(string ModelId);

internal sealed record RecordUpsert(string ModelId, IStateRecord Record) : RecordEdit(ModelId);

internal sealed record RecordDeletion(string ModelId, string RecordId) : RecordEdit(ModelId);
