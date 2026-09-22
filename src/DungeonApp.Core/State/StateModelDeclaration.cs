using System;

namespace DungeonApp.Core.State;

/// <summary>
/// One model a system's campaigns keep - a name, a version, and (through
/// <see cref="StateModelDeclaration{TRecord}"/>) the record type it stores. Non-generic so the
/// frame can hold a heterogeneous list of every model a system declares
/// (<c>IGameSystem.StateModels</c>) and save or load each one without ever naming a system's own
/// record type at compile time - see <see cref="RecordType"/>.
/// </summary>
public abstract class StateModelDeclaration
{
    private protected StateModelDeclaration(string modelId, int version, Type recordType)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(modelId);
        ArgumentOutOfRangeException.ThrowIfLessThan(version, 1);
        ArgumentNullException.ThrowIfNull(recordType);

        ModelId = modelId;
        Version = version;
        RecordType = recordType;
    }

    /// <summary>
    /// This model's permanent identity, prefixed by whoever owns it - <c>"entries.instances"</c>,
    /// not <c>"instances"</c> - so two systems, or a system and the library, can never collide on
    /// the same file name inside a campaign directory.
    /// </summary>
    public string ModelId { get; }

    /// <summary>
    /// This model's format version. A campaign whose stored version disagrees is refused rather
    /// than guessed at - docs/architecture.md, "Wersjonowanie": "Niezgodna wersja modelu oznacza,
    /// nie migruje."
    /// </summary>
    public int Version { get; }

    /// <summary>
    /// The concrete record type this model stores. Internal: only the frame's own save and load
    /// code (<c>JsonCampaignRepository</c>) ever reads it, to ask <c>System.Text.Json</c> to
    /// (de)serialize a record it never names itself.
    /// </summary>
    internal Type RecordType { get; }
}

/// <summary>
/// The declaration a system actually writes - <c>new StateModelDeclaration&lt;CampaignInstance&gt;
/// ("entries.instances", 1)</c> - typed by the record it stores, so
/// <see cref="CampaignChange.Upsert{TRecord}"/> and <see cref="CampaignStateSnapshot.Get{TRecord}"/>
/// return that same type back rather than a bare <see cref="IStateRecord"/> the caller would have
/// to cast.
/// </summary>
public sealed class StateModelDeclaration<TRecord> : StateModelDeclaration
    where TRecord : IStateRecord
{
    public StateModelDeclaration(string modelId, int version)
        : base(modelId, version, typeof(TRecord))
    {
    }
}
