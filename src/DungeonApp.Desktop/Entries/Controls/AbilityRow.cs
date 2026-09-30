using DungeonApp.Core.Entries;

namespace DungeonApp.Desktop.Entries.Controls;

/// <summary>
/// One box an <see cref="AbilityGridView"/> shows: a label, a score, and the score's own already-
/// formatted modifier (e.g. "+2", "−1") - three plain strings the card view supplies, exactly like
/// <see cref="TraitRow"/>. The modifier's sign is never colored differently by this control: the
/// library owns no meaning for "positive" or "negative" the way it owns Success/Warning/Danger
/// (docs/architecture.md, "Niezmiennik interfejsu" - every color used needs one documented meaning),
/// so both draw in the same neutral tone as any other secondary value.
/// </summary>
public sealed record AbilityRow(string Label, string Score, string Modifier);
