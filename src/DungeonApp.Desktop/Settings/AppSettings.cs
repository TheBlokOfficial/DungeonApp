using DungeonApp.Desktop.Themes;

namespace DungeonApp.Desktop.Settings;

/// <summary>
/// Persisted, app-level shell preferences. Distinct from campaign data: losing this file must
/// never block startup, so callers fall back to <see cref="Default"/> instead of throwing.
/// </summary>
public sealed record AppSettings(UiScaleProfile ScaleProfile)
{
    public static AppSettings Default { get; } = new(UiScaleProfile.Medium);
}
