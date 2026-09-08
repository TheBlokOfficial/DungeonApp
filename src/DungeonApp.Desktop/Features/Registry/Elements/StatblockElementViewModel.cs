using System.Collections.Generic;
using DungeonApp.Core.Content;

namespace DungeonApp.Desktop.Features.Registry.Elements;

/// <summary>
/// A rendered <see cref="StatblockElement"/>: an optional title and the rows an entry actually
/// supplies values for.
/// <para>
/// A trait whose field the entry left blank (legal only when the template marked that field
/// optional) contributes no row at all - not a row with an empty value - so nothing here ever shows
/// a label next to nothing. If that leaves zero rows, <see cref="TryCreate"/> returns <c>null</c>:
/// a statblock with nothing to say does not become an empty block on the card, it simply is not on
/// the card.
/// </para>
/// </summary>
public sealed class StatblockElementViewModel
{
    private StatblockElementViewModel(string? title, IReadOnlyList<StatblockRowViewModel> rows, bool isCompact)
    {
        Title = title;
        Rows = rows;
        IsCompact = isCompact;
    }

    public string? Title { get; }

    public bool HasTitle => !string.IsNullOrEmpty(Title);

    public IReadOnlyList<StatblockRowViewModel> Rows { get; }

    /// <summary>
    /// Carries <see cref="StatblockElement.Compact"/> straight through unchanged - the view model
    /// layer is not where the content-vs-layout decision gets made, only where it gets passed along
    /// to whatever the view does with it.
    /// </summary>
    public bool IsCompact { get; }

    /// <summary>
    /// Builds the rendered rows for one statblock element, or <c>null</c> when the entry supplied no
    /// value for any of its traits. <paramref name="fields"/> supplies each row's label - a card
    /// element only carries field addresses, never their display labels, so the caller hands in the
    /// owning template's field declarations once rather than every row re-resolving them.
    /// </summary>
    public static StatblockElementViewModel? TryCreate(
        StatblockElement element,
        IReadOnlyDictionary<FieldName, FieldDeclaration> fields,
        IReadOnlyDictionary<FieldName, FieldValue> values)
    {
        var rows = new List<StatblockRowViewModel>();

        foreach (var trait in element.Traits)
        {
            if (!values.TryGetValue(trait.Field, out var value))
            {
                continue;
            }

            string? secondary = null;

            if (trait.Secondary is { } secondaryField && values.TryGetValue(secondaryField, out var secondaryValue))
            {
                secondary = FieldValueText.Format(secondaryValue);
            }

            rows.Add(new StatblockRowViewModel(fields[trait.Field].Label, FieldValueText.Format(value), secondary));
        }

        return rows.Count == 0 ? null : new StatblockElementViewModel(element.Title, rows, element.Compact);
    }
}
