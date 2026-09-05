using System;

namespace DungeonApp.Core.DataBlocks;

/// <summary>
/// A transform's result did not match the shape its data block was registered with. Raised before
/// any state is touched, so the data block's previous value survives a rejected write untouched.
/// </summary>
public sealed class DataBlockShapeMismatchException(DataBlockId id, string message)
    : Exception($"Data block '{id}': {message}")
{
    public DataBlockId Id { get; } = id;
}
