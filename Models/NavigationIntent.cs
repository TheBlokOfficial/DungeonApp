namespace DungeonApp.Models;

public enum NavigationIntent
{
    Parallel,   // Przełączanie równorzędne (np. zakładki główne)
    DrillDown,  // Wejście w głąb hierarchii (np. widok detali)
    DrillUp     // Powrót w górę hierarchii
}
