namespace DungeonApp.ViewModels.Dashboard;

/// <summary>
/// Prosty model dla odznak (badges) wyświetlanych w widokach właściwości (np. Siła Ataku, Pancerz, HP).
/// Składa się z ikony (klucz z paczki lub aplikacji) i sformatowanego tekstu.
/// </summary>
public class PropertyBadgeViewModel
{
    public string? Icon { get; set; }
    public string Text { get; set; } = string.Empty;
    public string TextColor { get; set; } = "TextPrimary";

    public bool HasIcon => !string.IsNullOrWhiteSpace(Icon);
}
