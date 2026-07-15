namespace DungeonApp.Models.Campaigns.Engine.Modules.Timekeeper;

/// <summary>
/// Niezmiennik stanu czasowego kampanii, serializowany do pliku modules/Core.Timekeeper.json.
/// Używa modelu kalendarza Fantasy: 12 miesięcy × 30 dni = 360 dni/rok.
/// </summary>
public class TimeState
{
    public int Year   { get; set; } = 1;
    public int Month  { get; set; } = 1;
    public int Day    { get; set; } = 1;
    public int Hour   { get; set; } = 8;
    public int Minute { get; set; } = 0;
}
