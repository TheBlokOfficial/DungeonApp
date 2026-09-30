using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Media;
using DungeonApp.Core.Entries;
using DungeonApp.Desktop.Entries;
using DungeonApp.Testing;

namespace DungeonApp.Desktop.Tests.Entries;

/// <summary>
/// The way from an entry's picture path to what its frame is given: the picture read from the
/// entry's own pack directory, no picture, or the path as written when the file is missing or cannot
/// be read. The file is read by a stand-in decoder - the choice of state is what is tested here, not
/// the image codec.
/// </summary>
public sealed class EntryPictureTests : IDisposable
{
    private static readonly ContentTypeReference Pictured = new(ContentId.Create("sys"), ContentId.Create("thing"));

    private readonly TemporaryPacks _packs = new();

    public void Dispose() => _packs.Dispose();

    private sealed class Catalog(string? imageProperty) : IContentTypeCatalog
    {
        public bool HasSet(ContentId set) => set == Pictured.Set;

        public bool TryGet(ContentTypeReference reference, out ContentTypeDescriptor descriptor)
        {
            descriptor = new ContentTypeDescriptor(Pictured, "Thing", 1, imageProperty);
            return reference == Pictured;
        }

        public bool TryValidate(ContentTypeReference reference, ContentValues values, out string? error)
        {
            error = null;
            return true;
        }
    }

    private sealed class FakeImage : IImage
    {
        public FakeImage(string path) => Path = path;

        public string Path { get; }

        public Size Size => new(1, 1);

        public void Draw(DrawingContext context, Rect sourceRect, Rect destRect)
        {
        }
    }

    /// <summary>Reads only a file whose text is "picture"; anything else is not a picture.</summary>
    private static IImage Decode(string path) =>
        File.ReadAllText(path) == "picture" ? new FakeImage(path) : throw new InvalidDataException("not a picture");

    private async Task<(ContentRegistry Registry, RegisteredEntry Entry)> LoadAsync(string valuesJson, string? imageProperty = "picture")
    {
        _packs.WriteFile("cnt", "pack.json", """
            { "formatVersion": 1, "id": "cnt", "name": "Pack", "version": { "major": 1, "minor": 0 } }
            """);
        _packs.WriteFile("cnt", "entries/e.json", $$"""
            { "id": "e1", "name": "Entry", "template": "sys:thing", "templateVersion": 1, "values": {{valuesJson}} }
            """);

        var registry = await new ContentPackLoader(_packs.Path, new Catalog(imageProperty)).LoadAsync();
        return (registry, registry.Entries.Single());
    }

    [Fact]
    public async Task A_picture_file_in_the_pack_is_read_from_the_packs_own_directory()
    {
        _packs.WriteFile("cnt", "obrazy/a.png", "picture");
        var (registry, entry) = await LoadAsync("""{ "picture": "obrazy/a.png" }""");

        var picture = EntryPicture.Load(registry, entry, Decode);

        var image = Assert.IsType<FakeImage>(picture.Source);
        Assert.Equal(Path.GetFullPath(Path.Combine(_packs.Path, "cnt", "obrazy", "a.png")), image.Path);
        Assert.Null(picture.MissingPath);
    }

    [Fact]
    public async Task An_entry_naming_no_picture_has_none()
    {
        var (registry, entry) = await LoadAsync("{ }");

        Assert.Equal(EntryPicture.None, EntryPicture.Load(registry, entry, Decode));
    }

    [Fact]
    public async Task A_picture_property_the_type_does_not_declare_is_not_a_picture()
    {
        _packs.WriteFile("cnt", "a.png", "picture");
        var (registry, entry) = await LoadAsync("""{ "picture": "a.png" }""", imageProperty: null);

        Assert.Equal(EntryPicture.None, EntryPicture.Load(registry, entry, Decode));
    }

    [Fact]
    public async Task A_missing_file_is_reported_by_the_path_the_entry_wrote()
    {
        var (registry, entry) = await LoadAsync("""{ "picture": "obrazy\\brak.webp" }""");

        Assert.Equal(new EntryPicture(null, "obrazy\\brak.webp"), EntryPicture.Load(registry, entry, Decode));
    }

    [Fact]
    public async Task A_file_that_is_not_a_picture_is_reported_as_missing()
    {
        _packs.WriteFile("cnt", "a.png", "not a picture");
        var (registry, entry) = await LoadAsync("""{ "picture": "a.png" }""");

        Assert.Equal(new EntryPicture(null, "a.png"), EntryPicture.Load(registry, entry, Decode));
    }
}
