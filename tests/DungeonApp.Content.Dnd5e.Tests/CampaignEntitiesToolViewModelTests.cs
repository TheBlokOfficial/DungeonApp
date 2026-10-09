using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using DungeonApp.Core.Campaigns;
using DungeonApp.Core.Entries;
using DungeonApp.Core.Entries.Entities;
using DungeonApp.Desktop.Systems;
using DungeonApp.Desktop.Shell;
using DungeonApp.Desktop.Entries;
using DungeonApp.Desktop.Workspace.Layout;
using DungeonApp.Testing;

namespace DungeonApp.Content.Dnd5e.Tests;

/// <summary>
/// The first consumer of the <c>entries.instances</c> state model and <c>EntityResolver</c>
/// anywhere in the application. Every fixture builds its <see cref="ContentRegistry"/> by hand
/// rather than through <see cref="ContentPackLoader"/> - unlike <c>Dnd5eSystemTests</c>, this suite
/// is about the view model's own logic (which rows appear, which entries are offered, what a write
/// does), not about deserialization, so a hand-built registry keeps each test to exactly the
/// entries it needs.
/// </summary>
public sealed class CampaignEntitiesToolViewModelTests
{
    private static readonly Dnd5eSystem Dnd5e = new(
        [Path.Combine(Path.GetTempPath(), $"dnd5e-tool-tests-packs-{Guid.NewGuid():N}")]);

    [Fact]
    public async Task The_entity_list_reflects_every_entity_the_campaign_holds()
    {
        var fixture = new Fixture(ResolvedGear("pack", "potion", "Mikstura leczenia"));
        await fixture.AddEntityAsync(new EntryAddress(ContentId.Create("pack"), ContentId.Create("potion")), "Fiolka Toma");

        var viewModel = fixture.CreateViewModel();

        var row = Assert.Single(viewModel.Entities);
        Assert.Equal("Fiolka Toma", row.DisplayName);
        Assert.False(row.HasMessage);
    }

    [Fact]
    public async Task An_unnamed_entity_falls_back_to_the_entry_it_points_at()
    {
        var fixture = new Fixture(ResolvedGear("pack", "potion", "Mikstura leczenia"));
        await fixture.AddEntityAsync(new EntryAddress(ContentId.Create("pack"), ContentId.Create("potion")), label: null);

        var viewModel = fixture.CreateViewModel();

        Assert.Equal("Mikstura leczenia", Assert.Single(viewModel.Entities).DisplayName);
    }

    [Fact]
    public async Task An_entity_pointing_at_a_missing_pack_stays_on_the_list_and_is_marked()
    {
        var fixture = new Fixture();
        await fixture.AddEntityAsync(new EntryAddress(ContentId.Create("ghost-pack"), ContentId.Create("ghost-entry")), label: null);

        var viewModel = fixture.CreateViewModel();

        var row = Assert.Single(viewModel.Entities);
        Assert.True(row.HasMessage);
        Assert.Contains("ghost-pack", row.Message);
    }

    [Fact]
    public async Task Adding_a_selected_entry_creates_an_entity_and_saves_through_the_session()
    {
        var fixture = new Fixture(ResolvedGear("pack", "potion", "Mikstura leczenia"));
        var viewModel = fixture.CreateViewModel();
        var option = Assert.Single(viewModel.AddableEntries);

        viewModel.SelectedToAdd = option;
        await viewModel.AddCommand.ExecuteAsync(null);

        Assert.Equal(1, fixture.Repository.SaveCount);
        var entity = Assert.Single(fixture.Entities());
        Assert.Equal(option.Address, entity.Source);
        Assert.Null(viewModel.Message);

        // Cleared afterwards, so the same entry cannot be added twice by a second press on a
        // selection the GM already used.
        Assert.Null(viewModel.SelectedToAdd);
        Assert.False(viewModel.AddCommand.CanExecute(null));
    }

    [Fact]
    public void Adding_is_refused_while_nothing_is_selected()
    {
        var fixture = new Fixture(ResolvedGear("pack", "potion", "Mikstura leczenia"));

        var viewModel = fixture.CreateViewModel();

        Assert.False(viewModel.AddCommand.CanExecute(null));
        viewModel.SelectedToAdd = Assert.Single(viewModel.AddableEntries);
        Assert.True(viewModel.AddCommand.CanExecute(null));
    }

    [Fact]
    public void Addable_entries_exclude_unresolved_entries_and_entries_from_another_system()
    {
        var foreignSystem = ContentId.Create("other-set");
        var foreignReference = new ContentTypeReference(foreignSystem, ContentId.Create("thing"));
        var foreignEntry = new Entry(ContentId.Create("widget"), "Cudzy wpis", foreignReference, 1, ContentValues.Empty);
        var foreignRegistered = RegisteredEntry.CreateResolved(
            new EntryAddress(ContentId.Create("pack"), ContentId.Create("widget")),
            foreignEntry,
            new ContentTypeDescriptor(foreignReference, "Cokolwiek", 1));

        var unresolvedEntry = new Entry(
            ContentId.Create("broken"), "Zepsuty wpis", new ContentTypeReference(Dnd5e.ContentSetId, ContentId.Create("gear")), 1, ContentValues.Empty);
        var unresolvedRegistered = RegisteredEntry.CreateUnresolved(
            new EntryAddress(ContentId.Create("pack"), ContentId.Create("broken")),
            unresolvedEntry,
            EntryUnresolvedReason.ValuesRejected,
            "brak wymaganej wartości.");

        var fixture = new Fixture(ResolvedGear("pack", "potion", "Mikstura leczenia"), foreignRegistered, unresolvedRegistered);

        var viewModel = fixture.CreateViewModel();

        var option = Assert.Single(viewModel.AddableEntries);
        Assert.Equal("Mikstura leczenia", option.Name);
    }

    [Fact]
    public async Task Saving_current_hit_points_writes_a_sparse_patch_carrying_only_the_changed_field()
    {
        var fixture = new Fixture(ResolvedCreature("pack", "goblin", "Goblin", hp: 7));
        await fixture.AddEntityAsync(new EntryAddress(ContentId.Create("pack"), ContentId.Create("goblin")), label: null);

        var row = Assert.Single(fixture.CreateViewModel().Entities);
        Assert.True(row.CanEditHitPoints);

        row.CurrentHp = 3;
        await row.SaveHitPointsCommand.ExecuteAsync(null);

        var patch = Assert.Single(fixture.Entities()).Patch;
        Assert.False(patch.IsEmpty);

        // The patch names only the value that changed - the rest of the aspect stays with the entry.
        var keys = patch.Read<Dictionary<string, JsonElement>>();
        Assert.Equal(["combat"], keys.Keys);
        Assert.Equal("""{"currentHp":3}""", keys["combat"].GetRawText());
    }

    [Fact]
    public async Task After_saving_hit_points_the_merged_values_show_the_new_current_hp_and_the_entrys_max_is_untouched()
    {
        var fixture = new Fixture(ResolvedCreature("pack", "goblin", "Goblin", hp: 7));
        var address = new EntryAddress(ContentId.Create("pack"), ContentId.Create("goblin"));
        await fixture.AddEntityAsync(address, label: null);

        var row = Assert.Single(fixture.CreateViewModel().Entities);
        row.CurrentHp = 3;
        await row.SaveHitPointsCommand.ExecuteAsync(null);

        var entity = Assert.Single(fixture.Entities());
        var resolved = fixture.Context.Resolver.Resolve(entity);
        var combat = resolved.Values!.Read<Creature>().Combat!;

        Assert.Equal(3, combat.CurrentHp);
        Assert.Equal(7, combat.Hp);
    }

    [Fact]
    public async Task Setting_current_hit_points_back_to_the_entrys_own_value_leaves_the_patch_empty()
    {
        var fixture = new Fixture(ResolvedCreature("pack", "goblin", "Goblin", hp: 7));
        var address = new EntryAddress(ContentId.Create("pack"), ContentId.Create("goblin"));
        await fixture.AddEntityAsync(address, label: null);

        var firstRow = Assert.Single(fixture.CreateViewModel().Entities);
        firstRow.CurrentHp = 3;
        await firstRow.SaveHitPointsCommand.ExecuteAsync(null);

        var entityId = Assert.Single(fixture.Entities()).Id;
        Assert.False(fixture.Find(entityId)!.Patch.IsEmpty);

        // The entry never declares a current hp of its own, so clearing the customization back to
        // "nothing" is what "equal to the entry" means here.
        var secondRow = Assert.Single(fixture.CreateViewModel().Entities);
        Assert.Equal(3, secondRow.CurrentHp);
        secondRow.CurrentHp = null;
        await secondRow.SaveHitPointsCommand.ExecuteAsync(null);

        Assert.True(fixture.Find(entityId)!.Patch.IsEmpty);
    }

    [Fact]
    public async Task A_later_change_to_the_entrys_armor_class_reaches_an_entity_whose_hit_points_were_saved()
    {
        var address = new EntryAddress(ContentId.Create("pack"), ContentId.Create("goblin"));
        var original = new Fixture(ResolvedCreature("pack", "goblin", "Goblin", hp: 7, ac: 15));
        await original.AddEntityAsync(address, label: null);

        var row = Assert.Single(original.CreateViewModel().Entities);
        row.CurrentHp = 3;
        await row.SaveHitPointsCommand.ExecuteAsync(null);
        var patch = Assert.Single(original.Entities()).Patch;

        // The same campaign after the pack was corrected: the entry now ships another armor class.
        var corrected = new Fixture(ResolvedCreature("pack", "goblin", "Goblin", hp: 7, ac: 12));
        await corrected.AddEntityAsync(address, label: null);
        await corrected.Context.ChangeAsync(CampaignEntityChanges.ReplacePatch(Assert.Single(corrected.Entities()), patch));

        var resolved = corrected.Context.Resolver.Resolve(Assert.Single(corrected.Entities()));
        var combat = resolved.Values!.Read<Creature>().Combat!;

        Assert.Equal(12, combat.Ac);
        Assert.Equal(3, combat.CurrentHp);
    }

    [Fact]
    public async Task A_gear_entity_row_cannot_edit_hit_points()
    {
        var fixture = new Fixture(ResolvedGear("pack", "potion", "Mikstura leczenia"));
        await fixture.AddEntityAsync(new EntryAddress(ContentId.Create("pack"), ContentId.Create("potion")), label: null);

        var row = Assert.Single(fixture.CreateViewModel().Entities);

        Assert.False(row.CanEditHitPoints);
        Assert.False(row.SaveHitPointsCommand.CanExecute(null));
    }

    [Fact]
    public async Task An_unresolved_entity_row_cannot_edit_hit_points_and_stays_on_the_list()
    {
        var fixture = new Fixture();
        await fixture.AddEntityAsync(new EntryAddress(ContentId.Create("ghost-pack"), ContentId.Create("ghost-entry")), label: null);

        var row = Assert.Single(fixture.CreateViewModel().Entities);

        Assert.True(row.HasMessage);
        Assert.False(row.CanEditHitPoints);
        Assert.False(row.SaveHitPointsCommand.CanExecute(null));
    }

    [Fact]
    public async Task Removing_an_entity_takes_it_off_the_list_and_saves_through_the_session()
    {
        var fixture = new Fixture(ResolvedGear("pack", "potion", "Mikstura leczenia"));
        await fixture.AddEntityAsync(new EntryAddress(ContentId.Create("pack"), ContentId.Create("potion")), label: null);

        var viewModel = fixture.CreateViewModel();
        var row = Assert.Single(viewModel.Entities);

        await row.RemoveCommand.ExecuteAsync(null);

        Assert.Equal(2, fixture.Repository.SaveCount);
        Assert.Empty(viewModel.Entities);
        Assert.Empty(fixture.Entities());
    }

    [Fact]
    public async Task A_rows_commands_are_inactive_after_dispose()
    {
        var fixture = new Fixture(ResolvedCreature("pack", "goblin", "Goblin", hp: 7));
        await fixture.AddEntityAsync(new EntryAddress(ContentId.Create("pack"), ContentId.Create("goblin")), label: null);

        var row = Assert.Single(fixture.CreateViewModel().Entities);
        Assert.True(row.SaveHitPointsCommand.CanExecute(null));
        Assert.True(row.RemoveCommand.CanExecute(null));

        row.Dispose();

        Assert.False(row.SaveHitPointsCommand.CanExecute(null));
        Assert.False(row.RemoveCommand.CanExecute(null));
    }

    [Fact]
    public async Task Dispose_unsubscribes_so_a_later_entity_change_no_longer_refreshes_the_list()
    {
        var fixture = new Fixture();
        var viewModel = fixture.CreateViewModel();
        Assert.Empty(viewModel.Entities);

        viewModel.Dispose();
        await fixture.AddEntityAsync(new EntryAddress(ContentId.Create("pack"), ContentId.Create("potion")), label: null);

        Assert.Empty(viewModel.Entities);
    }

    private static RegisteredEntry ResolvedGear(string packId, string entryId, string name)
    {
        var reference = new ContentTypeReference(Dnd5e.ContentSetId, ContentId.Create("gear"));
        var entry = new Entry(ContentId.Create(entryId), name, reference, 1, ContentValues.From(new Gear { Rarity = "Pospolity", Category = "Mikstura", Item = new ItemAspect { Weight = 0.25m, Value = 50 } }));

        Assert.True(Dnd5e.TryGet(reference, out var descriptor));

        return RegisteredEntry.CreateResolved(new EntryAddress(ContentId.Create(packId), ContentId.Create(entryId)), entry, descriptor);
    }

    private static RegisteredEntry ResolvedCreature(string packId, string entryId, string name, int hp, int ac = 15)
    {
        var reference = new ContentTypeReference(Dnd5e.ContentSetId, ContentId.Create("creature"));
        var creature = new Creature
        {
            Size = "Mały",
            Type = "goblinoid",
            Alignment = "chaotyczne zło",
            Combat = new CombatAspect { Ac = ac, Hp = hp, Str = 8, Dex = 14, Con = 10, Int = 10, Wis = 8, Cha = 8 },
            Speed = "9 m",
            Senses = "wzrok w ciemności 18 m",
            Challenge = "1/4",
            Actions = new StatblockSection { Entries = [new StatblockEntry { Name = "Tasak", Text = "Tnie." }] },
        };
        var entry = new Entry(ContentId.Create(entryId), name, reference, 1, ContentValues.From(creature));

        Assert.True(Dnd5e.TryGet(reference, out var descriptor));

        return RegisteredEntry.CreateResolved(new EntryAddress(ContentId.Create(packId), ContentId.Create(entryId)), entry, descriptor);
    }

    private sealed class Fixture
    {
        public Fixture(params RegisteredEntry[] entries)
        {
            var packs = entries
                .Select(entry => entry.Address.Pack)
                .Distinct()
                .Select(packId => new Pack(
                    packId,
                    packId.ToString(),
                    new PackVersion(1, 0),
                    [.. entries.Where(entry => entry.Address.Pack == packId).Select(entry => entry.Entry)]))
                .ToArray();

            Registry = new ContentRegistry(packs, entries, [], []);
            Repository = new InMemoryCampaignRepository();

            var campaign = Campaign.Create(CampaignName.Create("Testowa"), TimeProvider.System);
            var session = new CampaignSession(campaign, Repository, [EntitiesModel.Declaration]);
            var tabContext = new CampaignTabContext(session);

            Context = new CampaignEntriesContext(tabContext, Registry, Dnd5e);
        }

        public ContentRegistry Registry { get; }

        public InMemoryCampaignRepository Repository { get; }

        public CampaignEntriesContext Context { get; }

        public CampaignEntitiesToolViewModel CreateViewModel() => new(Context, Dnd5e.ContentSetId);

        /// <summary>Every entity the campaign holds right now, read from the context's own snapshot.</summary>
        public IReadOnlyCollection<CampaignEntity> Entities() =>
            Context.Snapshot.Get(EntitiesModel.Declaration).Values.ToArray();

        public CampaignEntity? Find(EntityId id) =>
            Context.Snapshot.Get(EntitiesModel.Declaration).GetValueOrDefault(id.ToString());

        /// <summary>Brings an entry into the campaign the same way the view model's own "add" does - through the one door any change goes through - so a fixture's setup exercises exactly the path production code uses.</summary>
        public async Task AddEntityAsync(EntryAddress address, string? label) =>
            await Context.ChangeAsync(CampaignEntityChanges.Add(Context.Snapshot, address, label));
    }
}
