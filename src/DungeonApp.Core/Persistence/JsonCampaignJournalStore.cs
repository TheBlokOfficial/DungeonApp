using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using DungeonApp.Core.Campaigns;
using DungeonApp.Core.Journal;
using DungeonApp.Core.Modules;

namespace DungeonApp.Core.Persistence;

/// <summary>
/// Keeps the chronicle as one JSON object per line, in monthly files inside the campaign directory.
/// <para>
/// Line-delimited rather than one document: appending costs no rewrite however long the chronicle
/// grows, a line truncated by an interrupted write costs that line alone, and a field added to
/// future entries needs no migration of the past ones. None of that would be true of an array in a
/// single JSON file, which is exactly why the journal was kept out of the manifest.
/// </para>
/// </summary>
public sealed class JsonCampaignJournalStore(string libraryPath) : ICampaignJournalStore
{
    private const string JournalDirectoryName = "journal";
    private const string JournalSearchPattern = "*.jsonl";

    // Compact on purpose: one entry has to fit on one line.
    private readonly JsonSerializerOptions _serializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    public async Task AppendAsync(
        CampaignId campaign,
        IReadOnlyList<JournalEntry> entries,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entries);

        if (entries.Count == 0)
        {
            return;
        }

        var directory = GetJournalDirectory(campaign);
        Directory.CreateDirectory(directory);

        // Grouped by month so a long-running campaign never has one unbounded file, and so the
        // newest entries are always in the last file by name.
        foreach (var month in entries.GroupBy(entry => FileNameFor(entry.RecordedAt)))
        {
            cancellationToken.ThrowIfCancellationRequested();

            var lines = month.Select(entry => JsonSerializer.Serialize(ToLine(entry), _serializerOptions));

            await File.AppendAllLinesAsync(Path.Combine(directory, month.Key), lines, cancellationToken);
        }
    }

    public async Task<IReadOnlyList<JournalEntry>> ReadRecentAsync(
        CampaignId campaign,
        int limit,
        CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(limit);

        var directory = GetJournalDirectory(campaign);

        if (!Directory.Exists(directory))
        {
            return [];
        }

        var recent = new List<JournalEntry>(limit);

        // Newest file first, and only as many files as the limit actually needs.
        var files = Directory
            .EnumerateFiles(directory, JournalSearchPattern)
            .OrderByDescending(file => file, StringComparer.Ordinal);

        foreach (var file in files)
        {
            cancellationToken.ThrowIfCancellationRequested();

            string[] lines;

            try
            {
                lines = await File.ReadAllLinesAsync(file, cancellationToken);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                // A chronicle that cannot be read is a loss, not a failure: it never held state.
                continue;
            }

            for (var index = lines.Length - 1; index >= 0 && recent.Count < limit; index--)
            {
                if (TryReadLine(lines[index], out var entry))
                {
                    recent.Add(entry);
                }
            }

            if (recent.Count >= limit)
            {
                break;
            }
        }

        return recent;
    }

    /// <summary>
    /// A damaged line is skipped rather than thrown over. The chronicle explains the world; it does
    /// not hold it, so one unreadable entry must never stop the rest from being shown.
    /// </summary>
    private bool TryReadLine(string line, out JournalEntry entry)
    {
        entry = null!;

        if (string.IsNullOrWhiteSpace(line))
        {
            return false;
        }

        JournalLine? stored;

        try
        {
            stored = JsonSerializer.Deserialize<JournalLine>(line, _serializerOptions);
        }
        catch (JsonException)
        {
            return false;
        }

        if (stored?.Summary is null)
        {
            return false;
        }

        ModuleId? module = ModuleId.TryCreate(stored.Module, out var id) ? id : null;

        entry = new JournalEntry(stored.At, module, stored.Summary, stored.Reason);

        return true;
    }

    private static JournalLine ToLine(JournalEntry entry) =>
        new(entry.RecordedAt, entry.Module?.Value, entry.Summary, entry.Reason);

    private static string FileNameFor(DateTimeOffset moment) =>
        $"{moment.UtcDateTime.ToString("yyyy-MM", CultureInfo.InvariantCulture)}.jsonl";

    private string GetJournalDirectory(CampaignId campaign) =>
        Path.Combine(libraryPath, campaign.Value.ToString("D"), JournalDirectoryName);

    private sealed record JournalLine(DateTimeOffset At, string? Module, string? Summary, string? Reason);
}
