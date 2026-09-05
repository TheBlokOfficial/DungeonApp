using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using DungeonApp.Core.Campaigns;
using DungeonApp.Core.DataBlocks;
using DungeonApp.Core.Persistence;
using DungeonApp.Core.Tests.Fakes;

namespace DungeonApp.Core.Tests.Persistence;

public sealed class DataBlockPersistenceTests : IDisposable
{
    private static readonly DateTimeOffset Moment = new(2026, 8, 27, 18, 30, 0, TimeSpan.Zero);

    private static readonly DataBlockId RosterId = DataBlockId.Create("party.roster");
    private static readonly DataBlockId NoteId = DataBlockId.Create("party.note");

    private static readonly ObjectShape RosterShape = new(
    [
        new FieldShape("name", new PrimitiveShape(PrimitiveKind.Text)),
        new FieldShape("level", new PrimitiveShape(PrimitiveKind.Integer)),
    ]);

    private static readonly PrimitiveShape NoteShape = new(PrimitiveKind.Text);

    private readonly TemporaryLibrary _library = new();

    public void Dispose() => _library.Dispose();

    private static DataBlockRegistry Registry(int rosterVersion = 1) => new DataBlockRegistry()
        .Register(RosterId, rosterVersion, RosterShape)
        .Register(NoteId, 1, NoteShape);

    private JsonCampaignRepository Repository(DataBlockRegistry registry) => new(_library.Path, registry);

    private static Campaign NewCampaign(DataBlockRegistry registry) =>
        Campaign.Create(CampaignName.Create("Kroniki Doliny"), registry, new FixedTimeProvider(Moment));

    private string DataBlockValuePath(Campaign campaign, DataBlockId id) =>
        Path.Combine(_library.CampaignDirectory(campaign.Id.Value), "datablocks", $"{id.Value}.json");

    private JsonElement Manifest(Campaign campaign) =>
        JsonDocument.Parse(File.ReadAllText(_library.DocumentPath(campaign.Id.Value))).RootElement;

    [Fact]
    public async Task Gives_each_written_data_block_its_own_value_file()
    {
        var registry = Registry();
        var campaign = NewCampaign(registry);
        campaign.DataBlocks.Apply(NoteId, _ => "Remember the amulet.");

        await Repository(registry).SaveAsync(campaign);

        Assert.True(File.Exists(DataBlockValuePath(campaign, NoteId)));

        var entries = Manifest(campaign).GetProperty("dataBlocks").EnumerateArray().ToArray();
        var entry = Assert.Single(entries);
        Assert.Equal("party.note", entry.GetProperty("id").GetString());
        Assert.Equal(1, entry.GetProperty("version").GetInt32());
    }

    /// <summary>A data block never written never gets a file or a manifest entry.</summary>
    [Fact]
    public async Task Writes_no_file_for_a_data_block_that_was_never_written()
    {
        var registry = Registry();
        var campaign = NewCampaign(registry);

        await Repository(registry).SaveAsync(campaign);

        Assert.False(File.Exists(DataBlockValuePath(campaign, NoteId)));
        Assert.Empty(Manifest(campaign).GetProperty("dataBlocks").EnumerateArray());
    }

    [Fact]
    public async Task Reads_a_data_block_value_back_into_a_fresh_instance()
    {
        var registry = Registry();
        var campaign = NewCampaign(registry);
        campaign.DataBlocks.Apply(RosterId, _ => new System.Collections.Generic.Dictionary<string, object>
        {
            ["name"] = "Aria",
            ["level"] = 7,
        });

        await Repository(registry).SaveAsync(campaign);
        var reopened = await Repository(registry).GetAsync(campaign.Id);

        var roster = (System.Collections.Generic.IReadOnlyDictionary<string, object>)reopened!.DataBlocks.Read(RosterId)!;
        Assert.Equal("Aria", roster["name"]);
        Assert.Equal(7L, roster["level"]);
    }

    /// <summary>
    /// The whole reason PrimitiveKind splits Integer from Fractional: JSON has no wire-level
    /// distinction between the two, so without a declared shape to materialize against, an integer
    /// would silently come back from disk as a double - a bug that only shows up after a restart.
    /// </summary>
    [Fact]
    public async Task An_integer_value_round_trips_as_the_same_CLR_type_it_was_written_as()
    {
        var registry = Registry();
        var campaign = NewCampaign(registry);
        campaign.DataBlocks.Apply(RosterId, _ => new System.Collections.Generic.Dictionary<string, object>
        {
            ["name"] = "Aria",
            ["level"] = 7,
        });

        await Repository(registry).SaveAsync(campaign);
        var reopened = await Repository(registry).GetAsync(campaign.Id);

        var roster = (System.Collections.Generic.IReadOnlyDictionary<string, object>)reopened!.DataBlocks.Read(RosterId)!;
        Assert.IsType<long>(roster["level"]);
    }

    [Fact]
    public async Task Leaves_no_temporary_file_behind()
    {
        var registry = Registry();
        var campaign = NewCampaign(registry);
        campaign.DataBlocks.Apply(NoteId, _ => "A note.");

        await Repository(registry).SaveAsync(campaign);
        await Repository(registry).SaveAsync(campaign);

        var directory = _library.CampaignDirectory(campaign.Id.Value);
        Assert.Empty(Directory.EnumerateFiles(directory, "*.tmp", SearchOption.AllDirectories));
    }

    [Fact]
    public async Task Reports_a_value_file_that_lags_the_manifest()
    {
        var registry = Registry();
        var campaign = NewCampaign(registry);
        campaign.DataBlocks.Apply(NoteId, _ => "A note.");
        await Repository(registry).SaveAsync(campaign);

        var valuePath = DataBlockValuePath(campaign, NoteId);
        File.WriteAllText(valuePath, File.ReadAllText(valuePath).Replace("\"generation\": 1", "\"generation\": 0"));

        var exception = await Assert.ThrowsAsync<CampaignStoreException>(
            () => Repository(registry).GetAsync(campaign.Id));

        Assert.Equal(CampaignStoreFailure.TornSave, exception.Failure);
    }

    [Fact]
    public async Task Reports_a_value_file_that_is_missing_entirely()
    {
        var registry = Registry();
        var campaign = NewCampaign(registry);
        campaign.DataBlocks.Apply(NoteId, _ => "A note.");
        await Repository(registry).SaveAsync(campaign);

        File.Delete(DataBlockValuePath(campaign, NoteId));

        var exception = await Assert.ThrowsAsync<CampaignStoreException>(
            () => Repository(registry).GetAsync(campaign.Id));

        Assert.Equal(CampaignStoreFailure.TornSave, exception.Failure);
    }

    /// <summary>Listing draws the shelf from manifests alone, so a torn data block cannot hide a campaign.</summary>
    [Fact]
    public async Task Lists_a_campaign_whose_data_block_value_is_torn()
    {
        var registry = Registry();
        var campaign = NewCampaign(registry);
        campaign.DataBlocks.Apply(NoteId, _ => "A note.");
        await Repository(registry).SaveAsync(campaign);

        File.Delete(DataBlockValuePath(campaign, NoteId));

        var summary = Assert.Single(await Repository(registry).ListAsync());
        Assert.Equal(campaign.Id, summary.Id);
    }

    // --- Section 4: a data block this build cannot read is kept, never dropped, never refused. ---

    /// <summary>
    /// A save can legitimately mention a data block a different build's registry does not carry - an
    /// older build opening a newer save, or a block removed since. Opening must still succeed.
    /// </summary>
    [Fact]
    public async Task Opens_normally_when_the_save_names_a_data_block_unknown_to_this_registry()
    {
        var writer = Registry();
        var campaign = NewCampaign(writer);
        campaign.DataBlocks.Apply(RosterId, _ => new System.Collections.Generic.Dictionary<string, object>
        {
            ["name"] = "Aria",
            ["level"] = 7,
        });
        campaign.DataBlocks.Apply(NoteId, _ => "A note.");
        await Repository(writer).SaveAsync(campaign);

        // A reader that only knows about the note, not the roster.
        var reader = new DataBlockRegistry().Register(NoteId, 1, NoteShape);

        var reopened = await Repository(reader).GetAsync(campaign.Id);

        Assert.NotNull(reopened);
        Assert.Equal("A note.", reopened.DataBlocks.Read(NoteId));

        var reported = Assert.Single(reopened.DataBlocks.UnreadableBlocks);
        Assert.Equal(RosterId, reported.Id);
        Assert.Equal(DataBlockUnreadableReason.UnknownToRegistry, reported.Reason);
    }

    [Fact]
    public async Task Opens_normally_when_a_data_block_is_stored_at_an_unsupported_version()
    {
        var writer = Registry();
        var campaign = NewCampaign(writer);
        campaign.DataBlocks.Apply(RosterId, _ => new System.Collections.Generic.Dictionary<string, object>
        {
            ["name"] = "Aria",
            ["level"] = 7,
        });
        await Repository(writer).SaveAsync(campaign);

        // A reader whose roster shape moved to version 2 with no migration written yet.
        var reader = Registry(rosterVersion: 2);

        var reopened = await Repository(reader).GetAsync(campaign.Id);

        Assert.NotNull(reopened);
        var reported = Assert.Single(reopened.DataBlocks.UnreadableBlocks);
        Assert.Equal(RosterId, reported.Id);
        Assert.Equal(DataBlockUnreadableReason.UnsupportedVersion, reported.Reason);
    }

    [Fact]
    public async Task Apply_on_a_data_block_the_save_could_not_read_is_rejected()
    {
        var writer = Registry();
        var campaign = NewCampaign(writer);
        campaign.DataBlocks.Apply(RosterId, _ => new System.Collections.Generic.Dictionary<string, object>
        {
            ["name"] = "Aria",
            ["level"] = 7,
        });
        await Repository(writer).SaveAsync(campaign);

        var reader = new DataBlockRegistry().Register(NoteId, 1, NoteShape);
        var reopened = await Repository(reader).GetAsync(campaign.Id);

        Assert.Throws<DataBlockUnreadableException>(
            () => reopened!.DataBlocks.Apply(RosterId, _ => new System.Collections.Generic.Dictionary<string, object>
            {
                ["name"] = "Tampered",
                ["level"] = 1,
            }));
    }

    /// <summary>
    /// The heart of the rule: a save that no data block this session can read still has to leave that
    /// block's file and manifest entry exactly as they were, generation after generation.
    /// </summary>
    [Fact]
    public async Task An_unreadable_data_blocks_file_and_manifest_entry_survive_a_resave_untouched()
    {
        var writer = Registry();
        var campaign = NewCampaign(writer);
        campaign.DataBlocks.Apply(RosterId, _ => new System.Collections.Generic.Dictionary<string, object>
        {
            ["name"] = "Aria",
            ["level"] = 7,
        });
        await Repository(writer).SaveAsync(campaign);

        var valuePath = DataBlockValuePath(campaign, RosterId);
        var beforeResave = File.ReadAllText(valuePath);

        var reader = new DataBlockRegistry().Register(NoteId, 1, NoteShape);
        var reopened = await Repository(reader).GetAsync(campaign.Id);

        // A session that never learned to read the roster writes a note instead, then saves.
        reopened!.DataBlocks.Apply(NoteId, _ => "A new note.");
        await Repository(reader).SaveAsync(reopened);

        Assert.Equal(beforeResave, File.ReadAllText(valuePath));

        var entries = Manifest(reopened).GetProperty("dataBlocks").EnumerateArray().ToArray();
        var rosterEntry = Assert.Single(entries, entry => entry.GetProperty("id").GetString() == "party.roster");
        Assert.Equal(1, rosterEntry.GetProperty("version").GetInt32());
        Assert.Equal(1, rosterEntry.GetProperty("generation").GetInt64());

        var noteEntry = Assert.Single(entries, entry => entry.GetProperty("id").GetString() == "party.note");
        Assert.Equal(2, noteEntry.GetProperty("generation").GetInt64());
    }
}
