using DungeonApp.Core.State;

namespace DungeonApp.Core.World;

/// <summary>
/// One folder of the world tree: a name and the folder it stands in. The root ("Świat") is not a
/// record - <see cref="ParentId"/> of <see langword="null"/> means "directly under the root".
/// A state record like <see cref="Entries.Entities.CampaignEntity"/>: the deserializer is its only validator.
/// </summary>
public sealed record WorldFolder : IStateRecord
{
    public required FolderId Id { get; init; }

    public required string Name { get; init; }

    public FolderId? ParentId { get; init; }

    string IStateRecord.Id => Id.ToString();
}

/// <summary>
/// The campaign's single running number record: the highest № ever handed out. It only ever grows, so
/// deleting the entity that holds the highest number does not hand that number out again.
/// </summary>
public sealed record WorldCounter : IStateRecord
{
    /// <summary>The one record's key; the model never holds a second.</summary>
    public const string SingletonId = "counter";

    public required int LastNumber { get; init; }

    string IStateRecord.Id => SingletonId;
}
