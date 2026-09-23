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

    /// <summary>
    /// Komunikat pokazywany na pasku stanu, gdy ten krok zawiedzie i przerwie sekwencję startową
    /// (rama nigdy nie zna przyczyny awarii - to krok ją nazywa). Domyślny tekst, ogólny, wystarcza
    /// krokom samej ramy; krok zgłaszany przez system nadpisuje go własnym, konkretnym tekstem.
    /// </summary>
    string FailureWarning =>
        "Nie udało się w pełni przygotować startu aplikacji. Zostanie uruchomiona mimo to.";
}
