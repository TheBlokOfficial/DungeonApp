namespace DungeonApp.Core.DataBlocks;

/// <summary>
/// What this build knows about one data block: its identity, the version of its shape, and the
/// shape itself.
/// <para>
/// Carries no migration path on purpose. A migration is only meaningful once a shape's version has
/// actually been raised once; building that machinery ahead of the first real bump is building it
/// for a shape nobody has written yet.
/// </para>
/// </summary>
public sealed record DataBlockRegistration(DataBlockId Id, int Version, DataBlockShape Shape);
