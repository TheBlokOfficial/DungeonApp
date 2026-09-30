using System.Collections.Generic;

namespace DungeonApp.Core.Entries;

/// <summary>
/// What a content tab knows about whichever row is currently selected, once
/// <see cref="ContentListModel.Build"/> has confirmed the row is still visible - null otherwise: a
/// selection survives a filter change only while its row is still shown.
/// </summary>
public abstract record ContentSelectionDetail;

/// <summary>
/// A selected, resolved entry: exactly the header a detail view needs (category, name, tags), plus
/// the entry itself for whatever draws the actual card - the library never builds that card, see
/// <c>IContentPresentation</c>.
/// </summary>
public sealed record ValidEntrySelection(
    string? Category,
    string Name,
    IReadOnlyList<string> Tags,
    RegisteredEntry Entry) : ContentSelectionDetail;

/// <summary>
/// A selected broken row. The reason lives on <see cref="Row"/> itself - its
/// <see cref="ContentBrokenRow.UnresolvedEntry"/> (<see cref="RegisteredEntry.Unresolved"/>,
/// <see cref="RegisteredEntry.UnresolvedDetail"/>) or its <see cref="ContentBrokenRow.RejectedFile"/>
/// (<see cref="RejectedEntry.Reason"/>) - left unformatted, the same way <see cref="RejectedPack"/>'s
/// reason is: turning it into a Polish sentence is a view's job, not this model's.
/// </summary>
public sealed record BrokenRowSelection(ContentBrokenRow Row) : ContentSelectionDetail;

/// <summary>A selected rejected pack's header - <see cref="Pack"/>'s own <see cref="RejectedPack.Reason"/> is the reason.</summary>
public sealed record RejectedPackSelection(RejectedPack Pack) : ContentSelectionDetail;
