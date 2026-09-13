using DungeonApp.Core.Events;

namespace DungeonApp.Core.Content;

/// <summary>
/// A <see cref="CampaignInstance"/> was just added, removed, relabelled, or patched. Each carries
/// only the <see cref="InstanceId"/> - never the label, the patch, or the source, past or new.
/// <para>
/// A listener that wants to know what changed calls back into <see cref="CampaignInstances.Find"/>
/// for the current state; the event exists to say "look again", not to carry the look itself. That
/// keeps the announcement cheap regardless of how large an instance's patch is, and keeps these
/// event types stable as instances grow new things to carry - the same reasoning
/// <see cref="DataBlocks.DataBlockChanged"/> already applies to data blocks.
/// </para>
/// </summary>
public sealed record InstanceAdded(InstanceId Id) : ICampaignEvent;

/// <summary>An instance left the campaign. See <see cref="InstanceAdded"/> for why it carries only the id.</summary>
public sealed record InstanceRemoved(InstanceId Id) : ICampaignEvent;

/// <summary>An instance's GM-given name changed. See <see cref="InstanceAdded"/> for why it carries only the id.</summary>
public sealed record InstanceRelabelled(InstanceId Id) : ICampaignEvent;

/// <summary>An instance's patch over its entry was replaced. See <see cref="InstanceAdded"/> for why it carries only the id.</summary>
public sealed record InstancePatchReplaced(InstanceId Id) : ICampaignEvent;
