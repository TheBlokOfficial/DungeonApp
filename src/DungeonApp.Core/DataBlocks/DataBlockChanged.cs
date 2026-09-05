using DungeonApp.Core.Events;

namespace DungeonApp.Core.DataBlocks;

/// <summary>
/// A data block's value was just replaced. Carries only the id - never the value, past or new.
/// <para>
/// A listener that wants to know what changed calls back into <see cref="CampaignDataBlocks.Read"/>
/// for the current value; the event exists to say "look again", not to carry the look itself. That
/// keeps the announcement cheap regardless of how large a data block's value is, and keeps this
/// event class stable as new data block shapes are added.
/// </para>
/// </summary>
public sealed record DataBlockChanged(DataBlockId Id) : ICampaignEvent;
