using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using DungeonApp.Core.Journal;
using DungeonApp.Desktop.Shell;
using DungeonApp.Desktop.ViewModels;

namespace DungeonApp.Desktop.Features.CampaignWorkspace.Panels.History;

/// <summary>One line of the chronicle, as the GM reads it.</summary>
public sealed class ChronicleEntryViewModel(string at, string source, string summary, string? reason)
{
    public string At { get; } = at;

    /// <summary>Which module changed something, or the GM if they did it themselves.</summary>
    public string Source { get; } = source;

    public string Summary { get; } = summary;

    /// <summary>The grounds the change rested on. This is the half that makes the system explainable.</summary>
    public string? Reason { get; } = reason;

    public bool HasReason { get; } = !string.IsNullOrWhiteSpace(reason);
}

/// <summary>
/// The chronicle of what changed the world and why.
/// <para>
/// Read from disk rather than from the campaign in memory, because the campaign holds only the tail
/// that has not been written yet. It reloads whenever an operation commits, which covers a module
/// acting and the GM correcting something by hand alike.
/// </para>
/// </summary>
public sealed class HistoryPanelViewModel : ObservableObject, IDisposable
{
    /// <summary>
    /// Enough to answer "what just happened" several turns back. The chronicle is unbounded on
    /// disk; a panel that read all of it would get slower every session.
    /// </summary>
    private const int Limit = 100;

    private readonly CampaignSession _session;

    private bool _isLoading;

    private IReadOnlyList<ChronicleEntryViewModel> _entries;

    public HistoryPanelViewModel(
        CampaignSession session,
        IReadOnlyList<ChronicleEntryViewModel> initialEntries)
    {
        _session = session;
        _entries = initialEntries;
        _session.Committed += Reload;
    }

    public IReadOnlyList<ChronicleEntryViewModel> Entries
    {
        get => _entries;
        private set
        {
            if (SetField(ref _entries, value))
            {
                RaisePropertyChanged(nameof(IsEmpty));
            }
        }
    }

    public bool IsEmpty => !_isLoading && Entries.Count == 0;

    public void Dispose() => _session.Committed -= Reload;

    // Fire and forget from a synchronous notification. Reading the chronicle cannot fail in a way
    // that matters - the store already answers with an empty tail when it cannot read.
    private void Reload() => _ = ReloadAsync();

    private async Task ReloadAsync()
    {
        _isLoading = true;

        var entries = await _session.ReadChronicleAsync(Limit);

        Entries = ChroniclePresentation.Describe(_session.Campaign, entries);
        _isLoading = false;
        RaisePropertyChanged(nameof(IsEmpty));
    }
}

internal static class ChroniclePresentation
{
    public static IReadOnlyList<ChronicleEntryViewModel> Describe(
        DungeonApp.Core.Campaigns.Campaign campaign,
        IReadOnlyList<JournalEntry> entries)
    {
        var result = new ChronicleEntryViewModel[entries.Count];

        for (var index = 0; index < entries.Count; index++)
        {
            var entry = entries[index];
            result[index] = new ChronicleEntryViewModel(
                entry.RecordedAt.ToLocalTime().ToString("d MMM, HH:mm", CultureInfo.CurrentCulture),
                DescribeSource(campaign, entry),
                entry.Summary,
                entry.Reason);
        }

        return result;
    }

    /// <summary>
    /// Falls back to the raw identifier for a module the campaign no longer runs: its entries stay
    /// readable even when the module that wrote them is no longer active.
    /// </summary>
    private static string DescribeSource(
        DungeonApp.Core.Campaigns.Campaign campaign,
        JournalEntry entry)
    {
        if (entry.Module is not { } module)
        {
            return "MG";
        }

        return campaign.Modules.Find(module)?.Manifest.DisplayName ?? module.Value;
    }
}
