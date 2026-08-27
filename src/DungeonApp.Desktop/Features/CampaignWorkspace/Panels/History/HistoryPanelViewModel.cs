using System;
using System.Collections.ObjectModel;
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

    public HistoryPanelViewModel(CampaignSession session)
    {
        _session = session;
        _session.Committed += Reload;

        Reload();
    }

    public ObservableCollection<ChronicleEntryViewModel> Entries { get; } = [];

    public bool IsEmpty => !_isLoading && Entries.Count == 0;

    public void Dispose() => _session.Committed -= Reload;

    // Fire and forget from a synchronous notification. Reading the chronicle cannot fail in a way
    // that matters - the store already answers with an empty tail when it cannot read.
    private void Reload() => _ = ReloadAsync();

    private async Task ReloadAsync()
    {
        _isLoading = true;

        var entries = await _session.ReadChronicleAsync(Limit);

        Entries.Clear();

        foreach (var entry in entries)
        {
            Entries.Add(new ChronicleEntryViewModel(
                entry.RecordedAt.ToLocalTime().ToString("d MMM, HH:mm", CultureInfo.CurrentCulture),
                DescribeSource(entry),
                entry.Summary,
                entry.Reason));
        }

        _isLoading = false;
        RaisePropertyChanged(nameof(IsEmpty));
    }

    /// <summary>
    /// Falls back to the raw identifier for a module the campaign no longer runs: its entries stay
    /// in the chronicle, and an old line is still worth reading without the module that wrote it.
    /// </summary>
    private string DescribeSource(JournalEntry entry)
    {
        if (entry.Module is not { } module)
        {
            return "MG";
        }

        return _session.Campaign.Modules.Find(module)?.Manifest.DisplayName ?? module.Value;
    }
}
