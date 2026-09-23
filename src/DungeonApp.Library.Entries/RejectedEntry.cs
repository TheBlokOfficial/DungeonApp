namespace DungeonApp.Library.Entries;

/// <summary>
/// The same idea as <see cref="RejectedPack"/>, one level down: one entry file inside an otherwise
/// accepted pack that the loader refused to read as an entry at all, and why.
/// <para>
/// The two halves answer different questions and must not overlap. <see cref="Location"/> says
/// <em>which</em> file, and is the only place that file's own name belongs - the registry screen
/// shows it as the row's title. <see cref="Reason"/> says <em>what</em> is wrong, concretely, never
/// merely "invalid entry", and never repeats the file it is about: a reason that names its own file
/// prints that name twice on the same row, and every call site in
/// <see cref="ContentPackLoader"/> is written to keep it out.
/// </para>
/// <para>
/// The one deliberate exception is a duplicated entry id, where <see cref="Reason"/> lists the
/// <em>other</em> files declaring the same id but still not its own. Those other names are the only
/// thing that makes the defect fixable - the row cannot show them anywhere else - whereas its own
/// name is already right above the reason.
/// </para>
/// <para>
/// Unlike a rejected pack, a rejected entry never takes any sibling down with it: the rest of
/// <see cref="Pack"/> keeps loading and registering normally.
/// </para>
/// </summary>
public sealed record RejectedEntry(ContentId Pack, string Location, string Reason);
