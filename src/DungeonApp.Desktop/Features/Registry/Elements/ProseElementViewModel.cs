using System.Collections.Generic;
using DungeonApp.Core.Content;

namespace DungeonApp.Desktop.Features.Registry.Elements;

/// <summary>
/// A rendered <see cref="ProseElement"/>: an optional title and its text.
/// <para>
/// When the entry left the element's field blank (legal only when the template marked it optional),
/// there is no text to show and no empty block to show it in - <see cref="TryCreate"/> returns
/// <c>null</c>, and the element simply does not reach the card.
/// </para>
/// </summary>
public sealed class ProseElementViewModel
{
    private ProseElementViewModel(string? title, string text)
    {
        Title = title;
        Text = text;
    }

    public string? Title { get; }

    public bool HasTitle => !string.IsNullOrEmpty(Title);

    public string Text { get; }

    public static ProseElementViewModel? TryCreate(ProseElement element, IReadOnlyDictionary<FieldName, FieldValue> values)
    {
        if (!values.TryGetValue(element.Field, out var value))
        {
            return null;
        }

        return new ProseElementViewModel(element.Title, FieldValueText.Format(value));
    }
}
