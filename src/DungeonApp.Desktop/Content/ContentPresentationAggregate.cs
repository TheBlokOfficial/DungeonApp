using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Controls;
using DungeonApp.Core.Content;

namespace DungeonApp.Desktop.Content;

/// <summary>
/// The <see cref="IContentPresentation"/> the composition root hands to <see cref="Features.Registry.RegistryViewModel"/>:
/// splits on <see cref="ContentTypeReference.Set"/> to find the owning content set, then asks that
/// set to draw the card. The same list-not-a-single-instance shape as
/// <see cref="ContentTypeCatalogAggregate"/>, for the same reason.
/// </summary>
public sealed class ContentPresentationAggregate(IReadOnlyList<IContentSet> sets) : IContentPresentation
{
    public Control CreateCard(Entry entry)
    {
        var set = sets.FirstOrDefault(candidate => candidate.Id == entry.Type.Set);

        if (set is null)
        {
            // Callers must never reach here for an unresolved entry - a resolved RegisteredEntry
            // already proves some installed content set claimed this reference (ContentPackLoader
            // would not have resolved it otherwise), so a caller that checked Unresolved first can
            // only ever pass an entry this aggregate can serve.
            throw new InvalidOperationException(
                $"No installed content set matches '{entry.Type.Set}' for entry '{entry.Id}'. "
                + "CreateCard must never be called for an unresolved entry.");
        }

        return set.CreateCard(entry);
    }
}
