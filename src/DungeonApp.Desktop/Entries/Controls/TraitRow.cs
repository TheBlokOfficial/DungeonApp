namespace DungeonApp.Desktop.Entries.Controls;

/// <summary>
/// One row a <see cref="TraitListView"/> shows: a label, its value, and an optional second value
/// shown alongside it - "15" with its source "zbroja skórzana, tarcza" is one row carrying two
/// strings, not two rows. The card designer builds these directly in a system's card view; no
/// pack file ever names one.
/// <para>
/// <see cref="IsHighlighted"/> draws the value as an accent badge instead of text - the one value
/// in a list the card wants found at a glance (a difficulty rating). A layout decision of the card,
/// never of data.
/// </para>
/// </summary>
public sealed record TraitRow(string Label, string Value, string? Secondary = null, bool IsHighlighted = false)
{
    /// <summary>
    /// The secondary value in brackets, which is how every trad statblock on paper writes it:
    /// KP 15 (zbroja skórzana, tarcza), PZ 7 (2k6). Without them "PZ 7 2k6" is three numbers in a
    /// row and the reader has to guess where the value ends - a delimiter is what makes the pair
    /// readable, not decoration on top of it. Starts with the space that parts it from the value:
    /// both run on in one wrapping line of text.
    /// </summary>
    public string? SecondaryDisplay => Secondary is null ? null : $" ({Secondary})";

    /// <summary>
    /// A highlighted row's badge text: the value and its secondary value parted by a middle dot
    /// ("5 · 1 800 PD") - inside a badge, brackets would read as a second badge.
    /// </summary>
    public string BadgeText => Secondary is null ? Value : $"{Value} · {Secondary}";

    public bool IsPlain => !IsHighlighted;
}
