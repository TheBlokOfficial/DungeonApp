using System.Collections.Generic;

namespace DungeonApp.Core.Content;

/// <summary>
/// A system pack's declaration of one card shape: what fields an entry bound to this template must
/// or may supply, and what elements compose its card, in the order the card renders them.
/// <para>
/// <see cref="Fields"/> and <see cref="Card"/> keep the order they were written in the pack file.
/// That order is not incidental - it *is* the card's layout, per section 4 of the content
/// architecture doc ("układ karty przychodzi z danych") - so nothing here is free to resort it for
/// convenience.
/// </para>
/// <para>
/// <see cref="Version"/> is what an entry binds to and migrates through (section 17, axis 2).
/// <see cref="CatalogVersion"/> is the separate, slower axis: the version of the card element and
/// presentation contract catalog this template was written against (axis 1). The two never move
/// together on purpose.
/// </para>
/// </summary>
public sealed record Template(
    ContentId Id,
    string Name,
    int Version,
    int CatalogVersion,
    IReadOnlyList<FieldDeclaration> Fields,
    IReadOnlyList<CardElement> Card);
