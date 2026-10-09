using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Media;
using DungeonApp.Core.Entries;
using DungeonApp.Desktop.Entries;

namespace DungeonApp.Desktop.Systems;

/// <summary>
/// What the world catalog needs from the system's content to show and fill the tree: the loaded
/// entries, the catalog that validates their types, the badge colours and the library's row
/// profiles (for tags and badge of a result row). The frame never opens any of it beyond asking
/// those four questions.
/// </summary>
public sealed class WorldCatalogSource(
    ContentRegistry registry,
    IContentTypeCatalog types,
    IContentPresentation presentation,
    IReadOnlyList<IContentTypeProfile> profiles)
{
    public ContentRegistry Registry { get; } = registry;

    public IContentTypeCatalog Types { get; } = types;

    public IContentPresentation Presentation { get; } = presentation;

    public IReadOnlyList<IContentTypeProfile> Profiles { get; } = profiles;

    /// <summary>A system with no content: nothing to add, and every entity is unresolved.</summary>
    public static WorldCatalogSource Empty { get; } = new(
        new ContentRegistry([], [], [], []), new NoTypes(), new NoPresentation(), []);

    private sealed class NoTypes : IContentTypeCatalog
    {
        public bool HasSet(ContentId set) => false;

        public bool TryGet(ContentTypeReference reference, out ContentTypeDescriptor descriptor)
        {
            descriptor = default;
            return false;
        }

        public bool TryValidate(ContentTypeReference reference, ContentValues values, out string? error)
        {
            error = "Brak typów treści.";
            return false;
        }
    }

    private sealed class NoPresentation : IContentPresentation
    {
        public Control CreateCard(Entry entry, EntryPicture picture) => new TextBlock { Text = entry.Name };

        public IBrush? ResolveBadgeBrush(string colorKey) => null;
    }
}
