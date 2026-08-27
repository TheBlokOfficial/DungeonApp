namespace DungeonApp.Core.Events;

/// <summary>
/// A fact that has already happened, announced to whoever cares.
/// <para>
/// Events are named in the past tense and published only after the state they describe has changed.
/// They are never a way to ask for work: if the publisher needs to know that someone is listening,
/// it is not an announcement, and the call belongs on the other module's own interface.
/// </para>
/// </summary>
public interface ICampaignEvent;
