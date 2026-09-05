using System.Threading;
using System.Threading.Tasks;

namespace DungeonApp.Desktop.Startup;

/// <summary>
/// Jeden krok sekwencji startowej aplikacji. Cały kontrakt żyje w warstwie Desktop - <see
/// cref="ApplyAsync"/> dotyka drzewa Avalonii, więc rdzeń pozostaje nieświadomy istnienia startu.
/// Stan potrzebny między fazami trzyma krok we własnym prywatnym polu, nie generyczny mechanizm
/// współdzielony przez kontrakt.
/// </summary>
public interface IStartupStep
{
    /// <summary>Komunikat po polsku, pokazywany użytkownikowi zanim krok zacznie działać.</summary>
    string Describe();

    /// <summary>Praca bez dotykania UI: odczyt z dysku, sieć, obliczenia.</summary>
    Task PrepareAsync(CancellationToken cancellationToken);

    /// <summary>Zastosowanie efektu kroku na żywym drzewie interfejsu.</summary>
    Task ApplyAsync(StartupUiContext ui, CancellationToken cancellationToken);
}
