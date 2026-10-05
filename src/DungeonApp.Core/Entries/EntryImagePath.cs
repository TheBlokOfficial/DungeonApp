using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace DungeonApp.Core.Entries;

/// <summary>
/// An entry's picture path - the value of the property its content type declares as its picture
/// (<see cref="ContentTypeDescriptor.ImageProperty"/>). Two rules, and nothing else:
/// <list type="bullet">
/// <item><see cref="TryValidate"/>, at load time: the path is relative and stays inside the pack
/// once normalized, and names a PNG, JPEG, WebP or SVG file. A path failing either rejects the entry with
/// a reason, like any other broken entry (<see cref="ContentPackLoader"/>).</item>
/// <item><see cref="Locate"/>, when the picture is shown: the full path of the file inside the pack's
/// directory. Whether that file exists is not checked at load - a missing file does not break the
/// entry, it is shown as missing.</item>
/// </list>
/// Neither rule looks at which directory the pack came from: the GM's packs and the packs shipped
/// with the program are held to the same rules.
/// </summary>
public static class EntryImagePath
{
    /// <summary>The file extensions a picture may have, compared ignoring case.</summary>
    public static IReadOnlyList<string> Extensions { get; } = [".png", ".jpg", ".jpeg", ".webp", ".svg"];

    private static readonly char[] Separators = ['/', '\\'];

    public static bool TryValidate(string path, out string? error)
    {
        ArgumentNullException.ThrowIfNull(path);

        if (string.IsNullOrWhiteSpace(path))
        {
            error = "image path is empty.";
            return false;
        }

        // A drive letter ("C:obraz.png" is relative to that drive's current directory, not to the
        // pack) and a rooted path both point outside the pack before any segment is read.
        if (Path.IsPathRooted(path) || path.Contains(':', StringComparison.Ordinal))
        {
            error = $"image path '{path}' must be relative to the pack.";
            return false;
        }

        var depth = 0;

        foreach (var segment in path.Split(Separators))
        {
            if (segment == "..")
            {
                depth--;

                if (depth < 0)
                {
                    error = $"image path '{path}' leads outside the pack.";
                    return false;
                }
            }
            else if (segment.Length > 0 && segment != ".")
            {
                depth++;
            }
        }

        if (depth == 0 || path.EndsWith('/') || path.EndsWith('\\'))
        {
            error = $"image path '{path}' does not name a file.";
            return false;
        }

        var extension = Path.GetExtension(path);

        if (!Extensions.Contains(extension, StringComparer.OrdinalIgnoreCase))
        {
            error = $"image path '{path}' is not a PNG, JPEG, WebP or SVG file.";
            return false;
        }

        error = null;
        return true;
    }

    /// <summary>
    /// The picture path <paramref name="entry"/> names, as written in the entry - what a missing file
    /// is reported as. <see langword="null"/> when its type declares no picture or the entry names none.
    /// </summary>
    public static string? Declared(RegisteredEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);

        return entry.Type?.ImageProperty is { } property && entry.Entry.Values.TryGetString(property, out var path)
            ? path
            : null;
    }

    /// <summary>
    /// The picture of <paramref name="entry"/> as a full file path inside <paramref name="pack"/>'s
    /// directory, or <see langword="null"/> when there is no picture to show: the entry names none,
    /// or the pack was not read from disk. Does not touch the disk - whether the file is there is for
    /// whoever shows it to find out.
    /// </summary>
    public static string? Locate(Pack pack, RegisteredEntry entry)
    {
        ArgumentNullException.ThrowIfNull(pack);

        if (Declared(entry) is not { } path || pack.Location is not { } location)
        {
            return null;
        }

        return Path.GetFullPath(Path.Combine(location, path.Replace('\\', Path.DirectorySeparatorChar)));
    }
}
