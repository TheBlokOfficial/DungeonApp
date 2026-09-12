using DungeonApp.Core.Content;

namespace DungeonApp.Desktop.Features.Registry;

/// <summary>
/// One row of the registry's entry list. Wraps a <see cref="Content.RegisteredEntry"/> with the
/// display strings a row needs - including the owning pack's human name, which
/// <see cref="Content.RegisteredEntry"/> itself has no way to look up on its own.
/// </summary>
public sealed class RegistryEntryRowViewModel(RegisteredEntry registeredEntry, string packName)
{
    public RegisteredEntry RegisteredEntry { get; } = registeredEntry;

    public string Name => RegisteredEntry.Entry.Name;

    public string Address => RegisteredEntry.Address.ToString();

    public string PackName { get; } = packName;

    /// <summary>Empty for an unresolved entry - there is no content type to name.</summary>
    public string TypeName => RegisteredEntry.Type?.Name ?? string.Empty;

    public bool IsUnresolved => RegisteredEntry.Unresolved is not null;
}
