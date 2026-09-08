using System.Collections.Generic;

namespace DungeonApp.Core.Content;

/// <summary>
/// One section of a card, taken from the closed catalog described in section 7 of the content
/// architecture doc. Only the two variants a v1 statblock actually needs are here - a full
/// statblock (traits) and a block of static prose - the rest of the catalog (item lists, resource
/// bars, roll actions, binary markers) arrives in a later release, not by a pack inventing one.
/// <para>
/// Closed the same way <see cref="FieldValue"/> is: a private protected constructor means nothing
/// outside this file can add a third variant. Dispatch on which variant a given <see cref="CardElement"/>
/// is stays legal and necessary - the element catalog is closed and compiled, so a renderer has to
/// switch on it somewhere - what the architecture forbids is dispatch on what kind of *entry* is
/// being rendered, which never happens here.
/// </para>
/// </summary>
public abstract record CardElement
{
    private protected CardElement()
    {
    }
}

/// <summary>A list of traits, each an optional heading's worth of named values.</summary>
/// <param name="Compact">
/// A claim about the content, not about layout: these values are short and meant to be scanned,
/// not read as prose. It does not say how many columns, how wide, or anything else visual - that
/// stays the renderer's decision, free to change release over release without any pack needing an
/// update. A pack that wants a different visual arrangement has no parameter for it; this one isn't it.
/// </param>
public sealed record StatblockElement(string? Title, IReadOnlyList<StatblockTrait> Traits, bool Compact) : CardElement;

/// <summary>A single block of static text, read straight out of one of the entry's text fields.</summary>
public sealed record ProseElement(string? Title, FieldName Field) : CardElement;
