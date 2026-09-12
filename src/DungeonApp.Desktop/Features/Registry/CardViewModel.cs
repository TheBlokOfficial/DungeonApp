using System.Collections.Generic;
using System.Linq;
using DungeonApp.Core.Content;
using DungeonApp.Desktop.Features.Registry.Elements;

namespace DungeonApp.Desktop.Features.Registry;

/// <summary>
/// Composes one entry's card from a template and its raw field values, in exactly
/// <see cref="Template.Card"/>'s order - that order comes from the pack's own data and is the
/// card's layout, so nothing here is free to resort or filter it beyond dropping elements that end
/// up with nothing to show.
/// <para>
/// The switch below dispatches on <see cref="CardElement"/>'s variant, which is legal and required:
/// the element catalog is closed and compiled, so a card composer has to tell a statblock from a
/// prose block somewhere. What never happens here - and cannot, because the entry-facing API never
/// exposes it - is dispatch on what kind of entry is being rendered.
/// </para>
/// <para>
/// Unused since the content-registry pivot: <see cref="Entry"/> no longer carries a field-name-keyed
/// value dictionary (it carries an opaque <see cref="ContentValues"/> envelope instead), so this
/// type now takes that dictionary directly rather than pulling it out of a
/// <see cref="RegisteredEntry"/>. Kept, not deleted, per the brief for this pass - the next pass
/// removes it along with the rest of the old template-driven card machinery.
/// </para>
/// </summary>
public static class CardViewModel
{
    public static IReadOnlyList<object> Build(Template template, IReadOnlyDictionary<FieldName, FieldValue> values)
    {
        var fields = template.Fields.ToDictionary(field => field.Id);
        var elements = new List<object>();

        foreach (var cardElement in template.Card)
        {
            object? viewModel = cardElement switch
            {
                StatblockElement statblock => StatblockElementViewModel.TryCreate(statblock, fields, values),
                ProseElement prose => ProseElementViewModel.TryCreate(prose, values),
                _ => null
            };

            if (viewModel is not null)
            {
                elements.Add(viewModel);
            }
        }

        return elements;
    }
}
