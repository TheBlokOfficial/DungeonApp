namespace DungeonApp.Content.Dnd5e;

/// <summary>
/// One row of the campaign's instance list: the name to show and, when the instance did not resolve,
/// the Polish sentence explaining what is wrong. An unresolved instance is never left off this list -
/// it gets a row and a message instead, mirroring how the registry marks a broken entry rather than
/// hiding it (docs/architecture.md, "Co się dzieje, gdy treść jest zepsuta").
/// </summary>
public sealed record InstanceRowViewModel(string DisplayName, string? Message)
{
    public bool HasMessage => Message is not null;
}
