using System;
using System.Collections.Generic;

namespace DungeonApp.Core.Modules;

/// <summary>
/// What a module tells the campaign about itself.
/// <para>
/// <paramref name="StateVersion"/> is the module's own, independent of the campaign's format
/// version: adding a field to the clock must not force a migration of the whole save.
/// <paramref name="Requires"/> is declared rather than discovered, so an impossible combination is
/// refused when a campaign is created or opened instead of failing mid-session.
/// </para>
/// </summary>
public sealed record ModuleManifest(
    ModuleId Id,
    string DisplayName,
    int StateVersion,
    IReadOnlyList<ModuleId> Requires);

/// <summary>
/// A built-in unit of campaign behaviour, switched on per campaign. Not a plugin: modules ship with
/// the application and are enabled by configuration, never loaded from disk.
/// <para>
/// A module owns its state outright - nobody else interprets it. It does not own how that state is
/// stored: it hands over a plain object of its own type and never learns that JSON exists.
/// </para>
/// </summary>
public interface ICampaignModule
{
    ModuleManifest Manifest { get; }

    /// <summary>
    /// Called once, after every module in the campaign is registered and in dependency order, so a
    /// module can safely resolve the ones it declared.
    /// </summary>
    void OnActivated(ModuleContext context);

    /// <summary>The type <see cref="CaptureState"/> returns, so the store can deserialize into it.</summary>
    Type StateType { get; }

    object CaptureState();

    /// <summary>
    /// <paramref name="version"/> is the <see cref="ModuleManifest.StateVersion"/> the state was
    /// written at, which may be older than the current one. Migrating is the module's business.
    /// </summary>
    void RestoreState(object state, int version);
}
