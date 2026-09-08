namespace DungeonApp.Desktop.Features.Registry.Elements;

/// <summary>
/// One rendered row of a <see cref="StatblockElementViewModel"/>: a label, its formatted value, and
/// an optional second formatted value shown alongside it (a trait's <c>secondary</c> field) - "15"
/// with its source "zbroja skórzana, tarcza" is one row carrying two strings, not two rows.
/// </summary>
public sealed record StatblockRowViewModel(string Label, string Value, string? Secondary)
{
    public bool HasSecondary => Secondary is not null;

    /// <summary>
    /// The secondary value in brackets, which is how every trad statblock on paper writes it:
    /// KP 15 (zbroja skórzana, tarcza), PZ 7 (2k6), Wyzwanie 1/4 (50 PD). Without them "PZ 7 2k6"
    /// is three numbers in a row and the reader has to guess where the value ends - a delimiter is
    /// what makes the pair readable, not decoration on top of it.
    /// </summary>
    public string? SecondaryDisplay => Secondary is null ? null : $"({Secondary})";
}
