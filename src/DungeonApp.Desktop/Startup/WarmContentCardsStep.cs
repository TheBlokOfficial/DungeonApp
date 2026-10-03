using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DungeonApp.Core.Entries;
using DungeonApp.Desktop.Diagnostics;
using DungeonApp.Desktop.Entries;

namespace DungeonApp.Desktop.Startup;

/// <summary>
/// Warms one card for every resolved content type in <paramref name="registry"/> - a startup step the
/// system reports to the frame itself (<c>IGameSystem.StartupSteps</c>), right next to
/// <see cref="LoadContentPacksStep"/>: cards warm up behind the startup curtain, before anything can
/// be clicked, so none is built for the first time on the UI thread at the moment of a click.
/// <para>
/// <paramref name="registry"/> is deferred (<see cref="Func{TResult}"/>), not a plain value, because
/// this step is built before <see cref="LoadContentPacksStep"/> has read the packs - it reads the
/// registry only in its own <see cref="ApplyAsync"/>, once the loader is done.
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
                // No picture: warming builds the card's controls, frame included - reading a file
                // the GM may never open would only slow the start.
                var card = presentation.CreateCard(entry.Entry, EntryPicture.None);
                await VisualWarmupHost.AttachAndWaitAsync(ui.WarmupHost, card, cancellationToken);

                // The pieces a card lends the detail header are built with it, outside its own tree.
                if (card is IEntryCardHeader header)
                {
                    foreach (var piece in new[] { header.HeaderVisual, header.HeaderTitleEnd, header.HeaderTagsEnd, header.HeaderBlock })
                    {
                        if (piece is not null)
                        {
                            await VisualWarmupHost.AttachAndWaitAsync(ui.WarmupHost, piece, cancellationToken);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Warmup is an optimization - the card is still drawn for real on selection.
                AppLog.Error($"Rozgrzewka karty {entry.Entry.Type} nie powiodła się.", ex);
            }
        }
    }
}
