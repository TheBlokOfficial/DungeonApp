namespace DungeonApp.Core.Content;

/// <summary>
/// A pack's own version, declared in <c>pack.json</c> and later echoed in a campaign manifest's
/// <c>{ id, major }</c> content pack declaration (see section 12 of the content architecture doc).
/// <para>
/// Deliberately not compared or ordered here: what a minor bump versus a major bump means is a
/// decision for whoever resolves a campaign's declared packs against what is installed, not for
/// this type to bake in before that consumer exists.
/// </para>
/// </summary>
public readonly record struct PackVersion(int Major, int Minor)
{
    public override string ToString() => $"{Major}.{Minor}";
}
