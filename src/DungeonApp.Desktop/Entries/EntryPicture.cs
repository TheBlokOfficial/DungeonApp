using System;
using System.IO;
using System.Linq;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using DungeonApp.Desktop.Controls;
using DungeonApp.Core.Entries;

namespace DungeonApp.Desktop.Entries;

/// <summary>
/// An entry's picture, read for showing: the one place that turns the path an entry names
/// (<see cref="EntryImagePath"/>) into what a frame's <see cref="ImageFrame"/> is given. One of three
/// outcomes, matching the frame's three states:
/// <list type="bullet">
/// <item>the file was read - <see cref="Source"/> holds it;</item>
/// <item>the entry names no picture (or its type declares none) - both properties are
/// <see langword="null"/>, and the frame shows its placeholder icon;</item>
/// <item>the entry names a picture that is missing or cannot be read -
/// <see cref="MissingPath"/> holds the path as the entry wrote it, and the frame says so.</item>
/// </list>
/// The file is read here, when the picture is shown, never at load: a missing file does not break
/// the entry. The placeholder icon is not decided here - it is the system's, set by whoever places
/// the frame. Which directory the pack came from (the GM's own or shipped) makes no difference.
/// </summary>
public sealed record EntryPicture(IImage? Source, string? MissingPath)
{
    /// <summary>What the frame says when the picture's file is missing or cannot be read.</summary>
    public const string MissingMessage = "Brak pliku";

    /// <summary>No picture: the frame shows its placeholder icon.</summary>
    public static EntryPicture None { get; } = new(null, null);

    /// <summary>Reads <paramref name="entry"/>'s picture from its pack in <paramref name="registry"/>.</summary>
    public static EntryPicture Load(ContentRegistry registry, RegisteredEntry entry) =>
        Load(registry, entry, path => path.EndsWith(".svg", StringComparison.OrdinalIgnoreCase)
            ? SvgIcon.Load(path)
            : new Bitmap(path));

    /// <summary>
    /// As <see cref="Load(ContentRegistry, RegisteredEntry)"/>, with the file read by
    /// <paramref name="decode"/>, which throws when the file is missing or is not a picture it can read.
    /// </summary>
    public static EntryPicture Load(ContentRegistry registry, RegisteredEntry entry, Func<string, IImage> decode)
    {
        ArgumentNullException.ThrowIfNull(registry);
        ArgumentNullException.ThrowIfNull(entry);
        ArgumentNullException.ThrowIfNull(decode);

        if (EntryImagePath.Declared(entry) is not { } declared)
        {
            return None;
        }

        var pack = registry.Packs.FirstOrDefault(candidate => candidate.Id == entry.Address.Pack);

        // A missing file is checked for here rather than left to the decoder: not every decoder opens
        // the file (Avalonia's headless one does not), and "no such file" must not depend on which
        // one is in use.
        if (pack is null || EntryImagePath.Locate(pack, entry) is not { } path || !File.Exists(path))
        {
            return new EntryPicture(null, declared);
        }

        try
        {
            return new EntryPicture(decode(path), null);
        }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            // The file is the GM's, read from disk at the moment it is shown: a missing file, a locked
            // one and one that is not a picture the decoder can read (its failure has no single
            // exception type) are all the same thing to the GM - a picture that is not there.
            return new EntryPicture(null, declared);
        }
    }

    /// <summary>Hands this picture to <paramref name="frame"/> - every property the state is chosen by.</summary>
    public void ShowIn(ImageFrame frame)
    {
        ArgumentNullException.ThrowIfNull(frame);

        frame.Source = Source;
        frame.Message = MissingPath is null ? null : MissingMessage;
        frame.Detail = MissingPath;
    }
}
