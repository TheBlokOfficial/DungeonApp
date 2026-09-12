namespace DungeonApp.Desktop.Controls.Content;

/// <summary>
/// One row a <see cref="TraitListView"/> shows: a label, its value, and an optional second value
/// shown alongside it - "15" with its source "zbroja skórzana, tarcza" is one row carrying two
/// strings, not two rows. The card designer builds these directly in a content set's card view; no
/// pack file ever names one.
/// </summary>
public sealed record TraitRow(string Label, string Value, string? Secondary = null)
{
    public bool HasSecondary => Secondary is not null;

    /// <summary>
    /// The secondary value in brackets, which is how every trad statblock on paper writes it:
    /// KP 15 (zbroja skórzana, tarcza), PZ 7 (2k6). Without them "PZ 7 2k6" is three numbers in a
    /// row and the reader has to guess where the value ends - a delimiter is what makes the pair
    /// readable, not decoration on top of it.
    /// </summary>
    public string? SecondaryDisplay => Secondary is null ? null : $"({Secondary})";
}
