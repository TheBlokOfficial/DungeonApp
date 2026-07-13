using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Animation;

namespace DungeonApp.UI;

/// <summary>
/// Natychmiastowa tranzycja — zero animacji, zero opóźnienia.
/// </summary>
/// <remarks>
/// DLACZEGO nie używamy PageTransition = null:
/// Avalonia interpretuje null jako "użyj domyślnej tranzycji z tematu" (zazwyczaj CrossFade).
/// CrossFade potrzebuje minimum 1-2 klatek na fade-out/fade-in, co powoduje widoczny
/// micro-stutter przy przełączaniu zakładek. Ta klasa jest jawnym "brak animacji",
/// które TransitioningContentControl traktuje jako natychmiastową podmianę contentu.
/// </remarks>
public class InstantTransition : IPageTransition
{
    public static readonly InstantTransition Instance = new();

    public Task Start(Visual? from, Visual? to, bool forward, CancellationToken cancellationToken)
    {
        // Natychmiastowe ukrycie starego i pokazanie nowego — zero klatek oczekiwania.
        if (from != null) from.Opacity = 0;
        if (to != null) to.Opacity = 1;
        return Task.CompletedTask;
    }
}
