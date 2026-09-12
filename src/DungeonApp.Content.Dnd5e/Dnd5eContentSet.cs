using System;
using Avalonia.Controls;
using DungeonApp.Core.Content;
using DungeonApp.Desktop.Content;

namespace DungeonApp.Content.Dnd5e;

/// <summary>
/// The one place in the application allowed to know what a monster or a piece of gear is. Declares
/// two content types - <c>monster</c> ("Potwór", version 1) and <c>gear</c> ("Przedmiot", version 1)
/// - and builds their cards.
/// <para>
/// The dispatch on a content type's id inside <see cref="TryGet"/>, <see cref="TryValidate"/> and
/// <see cref="CreateCard"/> below is legal and necessary here: docs/architecture.md's "Kontrakty są
/// interfejsami" section names exactly one place allowed to be concrete about what a monster is, and
/// this is it. The ban that section and <c>CoreEntryKindIndependenceTests</c> enforce is on
/// <c>Core</c> or <c>Desktop</c> branching on entry kind - neither of them contains the word
/// "monster" anywhere, and neither ever will just because this switch exists.
/// </para>
/// </summary>
public sealed class Dnd5eContentSet : IContentSet
{
    private const string MonsterTypeId = "monster";
    private const string GearTypeId = "gear";

    private readonly ContentTypeDescriptor _monster;
    private readonly ContentTypeDescriptor _gear;

    public Dnd5eContentSet()
    {
        Id = ContentId.Create("dnd5e");
        _monster = new ContentTypeDescriptor(new ContentTypeReference(Id, ContentId.Create(MonsterTypeId)), "Potwór", 1);
        _gear = new ContentTypeDescriptor(new ContentTypeReference(Id, ContentId.Create(GearTypeId)), "Przedmiot", 1);
    }

    public ContentId Id { get; }

    public bool TryGet(ContentTypeReference reference, out ContentTypeDescriptor descriptor)
    {
        if (reference.Set == Id && reference.Type.Value == MonsterTypeId)
        {
            descriptor = _monster;
            return true;
        }

        if (reference.Set == Id && reference.Type.Value == GearTypeId)
        {
            descriptor = _gear;
            return true;
        }

        descriptor = default;
        return false;
    }

    public bool TryValidate(ContentTypeReference reference, ContentValues values, out string? error)
    {
        if (reference.Set != Id)
        {
            error = $"'{Id}' does not own content type reference '{reference}'.";
            return false;
        }

        try
        {
            switch (reference.Type.Value)
            {
                case MonsterTypeId:
                    values.Read<Monster>();
                    break;

                case GearTypeId:
                    values.Read<Gear>();
                    break;

                default:
                    error = $"'{Id}' declares no content type '{reference.Type}'.";
                    return false;
            }
        }
        catch (Exception ex)
        {
            // The deserializer is the validator (docs/architecture.md, "Deklaracja treści"): a
            // missing required value, an unknown key, or a value of the wrong shape all surface as
            // an exception here, and its message is the only explanation the GM ever sees
            // (RegisteredEntry.UnresolvedDetail).
            error = ex.Message;
            return false;
        }

        error = null;
        return true;
    }

    public Control CreateCard(Entry entry)
    {
        if (entry.Type.Set == Id && entry.Type.Type.Value == MonsterTypeId)
        {
            var view = new MonsterCardView();
            view.SetMonster(entry.Values.Read<Monster>());
            return view;
        }

        if (entry.Type.Set == Id && entry.Type.Type.Value == GearTypeId)
        {
            var view = new GearCardView();
            view.SetGear(entry.Values.Read<Gear>());
            return view;
        }

        throw new InvalidOperationException($"'{Id}' cannot draw a card for content type reference '{entry.Type}'.");
    }
}
