namespace DungeonApp.Models;

public class AppSettings
{
    /// <summary>
    /// Skala interfejsu (np. 1.0, 1.25). Wartość 0.0 oznacza Auto.
    /// </summary>
    public double UiScale { get; set; } = 0.0;

    /// <summary>
    /// Aktywny język interfejsu aplikacji (np. "en", "pl").
    /// </summary>
    public string Language { get; set; } = "en";

    /// <summary>
    /// Jednostka wagi (np. "kg", "lb").
    /// </summary>
    public string WeightUnit { get; set; } = "kg";

    /// <summary>
    /// Jednostka odległości (np. "ft.", "m.").
    /// </summary>
    public string DistanceUnit { get; set; } = "ft.";

    /// <summary>
    /// Tryb Deweloperski — wyświetla dodatkowe narzędzia (Workbench/Sandbox)
    /// przeznaczone do testowania i implementowania hermetycznych modułów UI.
    /// </summary>
    public bool IsDeveloperModeEnabled { get; set; } = false;
}
