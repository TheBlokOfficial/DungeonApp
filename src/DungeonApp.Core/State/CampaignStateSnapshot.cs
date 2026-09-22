using System;
using System.Collections.Generic;

namespace DungeonApp.Core.State;

/// <summary>
/// A campaign's whole state at one moment, read-only: every declared model, each one an immutable
/// set of records keyed by their own id (docs/architecture.md, "Gdzie mieszka stan": "Model to
/// niezmienny zbiór rzeczy kluczowany identyfikatorem"). A single record is simply a set with one
/// element in it - there is no separate "singleton model" kind.
/// <para>
/// Nothing here mutates. <see cref="Apply"/> returns a new snapshot rather than changing this one,
/// which is what lets <c>CampaignSession</c> hand this exact type to a change notification without
/// handing out anything a subscriber could use to write.
/// </para>
/// </summary>
public sealed class CampaignStateSnapshot
{
    private readonly IReadOnlyDictionary<string, IReadOnlyDictionary<string, IStateRecord>> _modelsById;

    private CampaignStateSnapshot(IReadOnlyDictionary<string, IReadOnlyDictionary<string, IStateRecord>> modelsById)
    {
        _modelsById = modelsById;
    }

    /// <summary>A campaign that has never held a single record in any model - what a brand new campaign starts with.</summary>
    public static CampaignStateSnapshot Empty { get; } =
        new(new Dictionary<string, IReadOnlyDictionary<string, IStateRecord>>());

    /// <summary>Built only by the frame's own load code, from whatever model files a campaign directory actually has.</summary>
    internal static CampaignStateSnapshot FromModels(
        IReadOnlyDictionary<string, IReadOnlyDictionary<string, IStateRecord>> modelsById) =>
        new(modelsById);

    /// <summary>Every model this snapshot carries at least one record for, keyed by model id - for the frame's own save code, which writes every declared model without ever naming a record type.</summary>
    internal IReadOnlyDictionary<string, IReadOnlyDictionary<string, IStateRecord>> ModelsById => _modelsById;

    /// <summary>
    /// <paramref name="declaration"/>'s records, keyed by their own id. A model nobody has ever
    /// written a record for comes back empty rather than missing - "Zadeklarowany model bez pliku
    /// → model pusty" is true of a snapshot exactly the way it is true of a campaign directory.
    /// </summary>
    public IReadOnlyDictionary<string, TRecord> Get<TRecord>(StateModelDeclaration<TRecord> declaration)
        where TRecord : IStateRecord
    {
        ArgumentNullException.ThrowIfNull(declaration);

        if (!_modelsById.TryGetValue(declaration.ModelId, out var records))
        {
            return new Dictionary<string, TRecord>();
        }

        var result = new Dictionary<string, TRecord>(records.Count);

        foreach (var (id, record) in records)
        {
            result[id] = (TRecord)record;
        }

        return result;
    }

    /// <summary>
    /// Returns a new snapshot with <paramref name="change"/>'s edits applied. Every model and every
    /// record this change does not name comes through unchanged - the fourth ban read as a data
    /// structure: nothing moves except what was handed to the change.
    /// </summary>
    public CampaignStateSnapshot Apply(CampaignChange change)
    {
        ArgumentNullException.ThrowIfNull(change);

        var updated = new Dictionary<string, Dictionary<string, IStateRecord>>();

        foreach (var (modelId, records) in _modelsById)
        {
            updated[modelId] = new Dictionary<string, IStateRecord>(records);
        }

        foreach (var edit in change.Edits)
        {
            if (!updated.TryGetValue(edit.ModelId, out var bucket))
            {
                updated[edit.ModelId] = bucket = [];
            }

            if (edit is RecordUpsert upsert)
            {
                bucket[upsert.Record.Id] = upsert.Record;
            }
            else if (edit is RecordDeletion deletion)
            {
                bucket.Remove(deletion.RecordId);
            }
        }

        var frozen = new Dictionary<string, IReadOnlyDictionary<string, IStateRecord>>(updated.Count);

        foreach (var (modelId, bucket) in updated)
        {
            frozen[modelId] = bucket;
        }

        return new CampaignStateSnapshot(frozen);
    }
}
