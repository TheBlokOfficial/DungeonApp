using System;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using DungeonApp.Core.Entries;
using DungeonApp.Core.State;
using DungeonApp.Core.World;
using DungeonApp.Desktop.Entries;
using DungeonApp.Desktop.Entries.ContentTab;
using DungeonApp.Desktop.Systems;

namespace DungeonApp.Desktop.Workspace.World;

/// <summary>An entity's card and the live part it shows, which its window keeps reading.</summary>
public sealed record EntityCard(ContentDetailViewModel Detail, EntityLiveContext Live);

/// <summary>
/// Turns an entity of the world tree into what the library's detail block draws: the system's own
/// card, built from the entry with the entity's values (the entry's overlaid with the entity's
/// patch), under the entry's library path with the entity's "№ 5" at its right edge, and given the
/// entity's live part (<see cref="IEntityLiveCard"/>). The system computes none of it - it only
/// draws the card. An entity whose entry is gone keeps its note under the reason.
/// </summary>
public sealed class EntityCardBuilder(WorldCatalogSource source, Func<CampaignChange, Task<CampaignChangeResult>> change)
{
    /// <summary>The card of <paramref name="id"/>; null when the tree no longer has it.</summary>
    public EntityCard? Build(WorldTree tree, EntityId id)
    {
        if (tree.Entity(id) is not { } node)
        {
            return null;
        }

        var number = $"№ {node.Entity.Number.ToString(CultureInfo.InvariantCulture)}";
        var resolved = node.Resolved;
        var live = new EntityLiveContext(resolved, source.Values, change);

        if (resolved is { Source: { } registered, Values: { } values, Unresolved: null })
        {
            var entry = registered.Entry with { Values = values };
            var tab = source.Tabs.FirstOrDefault(candidate => candidate.ContentTypes.Any(profile => profile.Type == entry.Type));
            var profile = tab?.ContentTypes.First(candidate => candidate.Type == entry.Type);

            // The live part goes in before the detail reads the card's header pieces.
            var card = source.Presentation.CreateCard(entry, EntryPicture.Load(source.Registry, registered));
            (card as IEntityLiveCard)?.ShowLive(live);

            return new EntityCard(
                new ValidContentDetailViewModel(
                    EntryPath.Of(tab?.Title ?? WorldCatalogViewModel.RootName, profile?.Category(entry), registered.Entry.Name),
                    node.Name,
                    profile?.Tags(entry) ?? [],
                    card,
                    number),
                live);
        }

        // The entry is gone, so the path is where it was looked for: the pack and the entry's id.
        return new EntityCard(
            new BrokenContentDetailViewModel(
                string.Empty,
                node.Name,
                Describe(resolved),
                path: null,
                breadcrumbs: [node.Entity.Source.Pack.ToString(), node.Entity.Source.Entry.ToString()],
                numberLabel: number,
                liveSections: live.CreateSections()),
            live);
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
