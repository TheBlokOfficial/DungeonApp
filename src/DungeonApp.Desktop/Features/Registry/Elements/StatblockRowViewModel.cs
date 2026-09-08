namespace DungeonApp.Desktop.Features.Registry.Elements;

/// <summary>
/// One rendered row of a <see cref="StatblockElementViewModel"/>: a label, its formatted value, and
/// an optional second formatted value shown alongside it (a trait's <c>secondary</c> field) - "15"
/// with its source "zbroja skórzana, tarcza" is one row carrying two strings, not two rows.
/// </summary>
public sealed record StatblockRowViewModel(string Label, string Value, string? Secondary)
{
    public bool HasSecondary => Secondary is not null;
}
