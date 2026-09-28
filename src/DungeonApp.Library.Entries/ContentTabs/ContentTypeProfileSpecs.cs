using System;
using System.Collections.Generic;

namespace DungeonApp.Library.Entries;

/// <summary>
/// A content type's category, as the system declares it: the name its filter chip carries in a
/// content tab ("Grupa") and compiled code reading the category out of the system's own record
/// (<typeparamref name="TRecord"/>) - null for a record with no category. A content type with no
/// category at all declares no spec, and a tab none of whose types declares one has no category
/// filter.
/// </summary>
public sealed record ContentCategorySpec<TRecord>(string Label, Func<TRecord, string?> Value);

/// <summary>
/// One filterable dimension over a content type's own values, as the system declares it: a label
/// for the filter, compiled code reading the system's own record (<typeparamref name="TRecord"/>),
/// and the order its options should list in - the system's choice, never alphabetical by accident
/// (a numeric rating orders numerically, a named tier orders by rank).
/// <see cref="ContentTypeProfile{TRecord}"/> is the only place this is ever consumed; the library
/// itself only ever sees the erased <see cref="ContentValueFilterDefinition"/> it produces.
/// </summary>
public sealed record ContentValueFilterSpec<TRecord>(
    string Label,
    Func<TRecord, string?> Value,
    IComparer<string> OptionOrder);

/// <summary>
/// One sort a content type offers beyond the library's own default name sort, as the system
/// declares it: a label and compiled code comparing two of its own records directly - see
/// <see cref="ContentValueFilterSpec{TRecord}"/>'s remarks for why this is typed and where it is
/// consumed.
/// </summary>
public sealed record ContentSortSpec<TRecord>(string Label, Comparison<TRecord> Compare);
