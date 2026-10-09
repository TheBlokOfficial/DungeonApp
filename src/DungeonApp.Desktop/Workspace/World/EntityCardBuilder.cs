using System.Globalization;
using System.Linq;
using DungeonApp.Core.Entries;
using DungeonApp.Core.World;
using DungeonApp.Desktop.Entries;
using DungeonApp.Desktop.Entries.ContentTab;
using DungeonApp.Desktop.Systems;

namespace DungeonApp.Desktop.Workspace.World;

/// <summary>
/// Turns an entity of the world tree into what the library's detail block draws: the system's own
/// card, built from the entry with the entity's values (the entry's overlaid with the entity's
/// patch), under the path of the entity's folders, with "№ 5 · Goblin" under the name. The system
/// computes none of it - it only draws the card.
/// </summary>
public sealed class EntityCardBuilder(WorldCatalogSource source)
{
    /// <summary>The detail of <paramref name="id"/>; null when the tree no longer has it.</summary>
    public ContentDetailViewModel? Build(WorldTree tree, EntityId id)
    {
        if (tree.Entity(id) is not { } node)
        {
            return null;
        }

        var number = node.Entity.Number.ToString(CultureInfo.InvariantCulture);
        var resolved = node.Resolved;

        if (resolved is { Source: { } registered, Values: { } values, Unresolved: null })
        {
            var entry = registered.Entry with { Values = values };
            var profile = source.Profiles.FirstOrDefault(candidate => candidate.Type == entry.Type);

            return new ValidContentDetailViewModel(
                [WorldCatalogViewModel.RootName, .. tree.EntityPath(id), node.Name],
                node.Name,
                profile?.Tags(entry) ?? [],
                source.Presentation.CreateCard(entry, EntryPicture.Load(source.Registry, registered)),
                $"№ {number} · {entry.Name}");
        }

        return new BrokenContentDetailViewModel(
            $"№ {number}",
            node.Name,
            Describe(resolved),
            path: null);
    }

    private static string Describe(ResolvedEntity resolved) => resolved.Unresolved switch
    {
        EntityUnresolvedReason.MissingPack => "Paczka tego wpisu nie jest zainstalowana.",
        EntityUnresolvedReason.MissingEntry => "Paczka nie zawiera już wpisu, z którego ta pozycja pochodzi.",
        EntityUnresolvedReason.EntryUnresolved => "Wpis, z którego ta pozycja pochodzi, nie został wczytany.",
        EntityUnresolvedReason.ValuesRejected => $"Wartości tej pozycji nie przeszły sprawdzenia: {resolved.UnresolvedDetail}",
        _ => "Nie udało się odczytać tej pozycji.",
    };
}
