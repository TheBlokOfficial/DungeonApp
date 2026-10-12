using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Avalonia.Media;
using DungeonApp.Desktop.Entries;

namespace DungeonApp.Content.Dnd5e;

/// <summary>
/// The icon of what kind of creature or item an entry is: a creature's type from the rulebook and an
/// item's category. Drawn small before the name in the content tab's rows and in place of a missing
/// picture on the card, so the GM tells a dragon from a plant at a glance. One family of filled
/// silhouettes (game-icons.net, list in <c>Icons/SOURCE.md</c>), embedded in this assembly: the icons
/// belong to the system, not to a pack, so they show for every pack's entries.
/// <para>
/// A type or category this system does not know - a pack's own word, a typo - gets the default of
/// its content type (a skull, a backpack), never nothing: every row keeps the same column.
/// </para>
/// </summary>
public static class KindIcons
{
    /// <summary>The icon of a creature whose type is not on <see cref="CreatureTypes"/>.</summary>
    public const string DefaultCreatureIcon = "stworzenie";

    /// <summary>The icon of an item whose category is not on <see cref="GearCategories"/>.</summary>
    public const string DefaultGearIcon = "ekwipunek";

    /// <summary>
    /// The rulebook's fourteen creature types as the pack writes them (base type, lower case), each
    /// with its icon's file name.
    /// </summary>
    public static IReadOnlyList<(string Type, string Icon)> CreatureTypes { get; } =
    [
        ("aberracja", "aberracja"),
        ("bestia", "bestia"),
        ("czart", "czart"),
        ("fey", "fey"),
        ("gigant", "gigant"),
        ("humanoid", "humanoid"),
        ("konstrukt", "konstrukt"),
        ("monstrum", "monstrum"),
        ("niebianin", "niebianin"),
        ("nieumarły", "nieumarly"),
        ("roślina", "roslina"),
        ("smok", "smok"),
        ("szlam", "szlam"),
        ("żywiołak", "zywiolak"),
    ];

    /// <summary>The item categories this system draws, as the pack writes them, each with its icon's file name.</summary>
    public static IReadOnlyList<(string Category, string Icon)> GearCategories { get; } =
    [
        ("Amunicja", "amunicja"),
        ("Broń", "bron"),
        ("Ekwipunek", "ekwipunek"),
        ("Kostur", "kostur"),
        ("Laska", "laska"),
        ("Mikstura", "mikstura"),
        ("Narzędzia", "narzedzia"),
        ("Pierścień", "pierscien"),
        ("Przedmiot cudowny", "przedmiot-cudowny"),
        ("Różdżka", "rozdzka"),
        ("Zbroja", "zbroja"),
        ("Zwój", "zwoj"),
    ];

    private static readonly CultureInfo Polish = CultureInfo.GetCultureInfo("pl-PL");

    private static readonly Dictionary<string, string> CreatureIconByType = ToLookup(CreatureTypes);

    private static readonly Dictionary<string, string> GearIconByCategory = ToLookup(GearCategories);

    private static readonly ConcurrentDictionary<string, DrawingImage> Loaded = new(StringComparer.Ordinal);

    /// <summary>
    /// The icon file name for a creature's type: the base type before a subtype in brackets
    /// ("humanoid (dowolna rasa)" is a humanoid, "czart (diabeł)" a fiend), and a swarm
    /// ("rój Malutkich bestii") is drawn as a beast.
    /// </summary>
    public static string CreatureIconName(string? type)
    {
        var baseType = (type ?? "").Split('(')[0].Trim().ToLower(Polish);

        if (baseType.StartsWith("rój ", StringComparison.Ordinal))
        {
            baseType = "bestia";
        }

        return CreatureIconByType.GetValueOrDefault(baseType, DefaultCreatureIcon);
    }

    /// <summary>The icon file name for an item's category, compared ignoring case.</summary>
    public static string GearIconName(string? category) =>
        GearIconByCategory.GetValueOrDefault((category ?? "").Trim().ToLower(Polish), DefaultGearIcon);

    /// <summary>The icon of a creature's type (<see cref="CreatureIconName"/>).</summary>
    public static DrawingImage ForCreature(string? type) => Load(CreatureIconName(type));

    /// <summary>The icon of an item's category (<see cref="GearIconName"/>).</summary>
    public static DrawingImage ForGear(string? category) => Load(GearIconName(category));

    /// <summary>
    /// The icon with file name <paramref name="name"/>, read from this assembly once and shared: a
    /// drawing is not a control, so one serves every row and card.
    /// </summary>
    public static DrawingImage Load(string name) => Loaded.GetOrAdd(name, static key =>
    {
        var resource = $"DungeonApp.Content.Dnd5e.Icons.{key}.svg";
        using var stream = typeof(KindIcons).Assembly.GetManifestResourceStream(resource)
            ?? throw new InvalidOperationException($"The icon '{resource}' is not embedded in this assembly.");
        using var reader = new StreamReader(stream);
        return SvgIcon.Parse(reader.ReadToEnd());
    });

    private static Dictionary<string, string> ToLookup(IReadOnlyList<(string Name, string Icon)> names)
    {
        var lookup = new Dictionary<string, string>(StringComparer.Ordinal);

        foreach (var (name, icon) in names)
        {
            lookup.Add(name.ToLower(Polish), icon);
        }

        return lookup;
    }
}
