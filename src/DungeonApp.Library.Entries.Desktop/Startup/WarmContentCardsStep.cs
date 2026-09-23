using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DungeonApp.Core.Content;
using DungeonApp.Desktop.Startup;
using DungeonApp.Library.Entries.Desktop.Content;

namespace DungeonApp.Library.Entries.Desktop.Startup;

/// <summary>
/// Rozgrzewa jedną kartę na każdy rozwiązany typ treści w <paramref name="registry"/> - krok startowy,
/// który system sam zgłasza ramie (<c>IGameSystem.StartupSteps</c>), tuż obok
/// <see cref="LoadContentPacksStep"/>: to, co "wybór systemu" dawniej budowało na wątku UI w chwili
/// kliknięcia, rozgrzewa się teraz za kurtyną startową, zanim cokolwiek da się kliknąć.
/// <para>
/// <paramref name="registry"/> jest odroczone (<see cref="Func{TResult}"/>), nie wartością wprost, bo
/// ten krok jest budowany zanim <see cref="LoadContentPacksStep"/> zdąży wczytać paczki - odczytuje
/// rejestr dopiero we własnym <see cref="ApplyAsync"/>, gdy loader już skończył.
/// </para>
/// </summary>
public sealed class WarmContentCardsStep(Func<ContentRegistry> registry, IContentPresentation presentation) : IStartupStep
{
    public string Describe() => "Rozgrzewanie kart treści…";

    public string FailureWarning => "Nie udało się rozgrzać kart treści.";

    public Task PrepareAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    /// <summary>
    /// One card per distinct, resolved content type among the loaded entries - the first entry found
    /// for each. A type with no entry in any installed pack has nothing to build a card from and is
    /// skipped.
    /// </summary>
    public async Task ApplyAsync(StartupUiContext ui, CancellationToken cancellationToken)
    {
        var samples = registry().Entries
            .Where(entry => entry.Type is not null)
            .GroupBy(entry => entry.Type!.Value.Reference)
            .Select(group => group.First());

        foreach (var entry in samples)
        {
            try
            {
                var card = presentation.CreateCard(entry.Entry);
                await VisualWarmupHost.AttachAndWaitAsync(ui.WarmupHost, card, cancellationToken);
            }
            catch (Exception)
            {
                // Warmup is an optimization - the registry screen still draws the card for real on selection.
            }
        }
    }
}
