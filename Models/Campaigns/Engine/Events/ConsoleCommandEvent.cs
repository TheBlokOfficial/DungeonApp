namespace DungeonApp.Models.Campaigns.Engine.Events;

/// <summary>
/// Zdarzenie emitowane przez ConsolModule gdy DM wpisze komendę zaczynającą się od '/'.
/// Każdy moduł subskrybujący ten event może samodzielnie zdecydować, czy dana komenda
/// go dotyczy — np. TimekeeperModule nasłuchuje prefixu "/time".
/// </summary>
/// <remarks>
/// DLACZEGO event zamiast bezpośredniego parsowania w ConsoleModule:
/// Moduły mają być hermetyczne. ConsoleModule nie powinien wiedzieć, jakie moduły
/// istnieją ani jakie komendy obsługują. Wyrzucenie komendy na szynę pozwala
/// na dodawanie nowych modułów z własnymi komendami bez modyfikowania konsoli.
/// </remarks>
public class ConsoleCommandEvent : CampaignEventBase
{
    /// <summary>
    /// Surowy tekst wpisany przez DM-a, np. "/time +8h" lub "/roll 2d6".
    /// </summary>
    public string RawInput { get; init; } = string.Empty;

    public ConsoleCommandEvent(string rawInput)
    {
        RawInput = rawInput;
    }
}
