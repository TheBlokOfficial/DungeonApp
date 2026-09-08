namespace DungeonApp.Core.Content;

/// <summary>
/// A pack directory the loader refused to install, in its entirety, and why.
/// <para>
/// <see cref="Reason"/> is diagnostic text for whoever reads the registry after a load (today: a
/// developer or the future registry screen), not user-facing copy - it exists to name a file and a
/// concrete defect ("templates/humanoid.json: field id 'kp' is declared twice"), never to just say
/// "invalid pack".
/// </para>
/// </summary>
public sealed record RejectedPack(string Location, string Reason);
