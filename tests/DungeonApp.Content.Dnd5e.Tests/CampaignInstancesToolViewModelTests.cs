using System;
using System.Linq;
using System.Threading.Tasks;
using DungeonApp.Core.Campaigns;
using DungeonApp.Core.Content;
using DungeonApp.Core.DataBlocks;
using DungeonApp.Desktop.Content;
using DungeonApp.Desktop.Shell;

namespace DungeonApp.Content.Dnd5e.Tests;

/// <summary>
/// The first consumer of <c>CampaignInstances</c> and <c>InstanceResolver</c> anywhere in the
/// application. Every fixture builds its <see cref="ContentRegistry"/> by hand rather than through
/// <see cref="ContentPackLoader"/> - unlike <c>Dnd5eContentSetTests</c>, this suite is about the view
/// model's own logic (which rows appear, which entries are offered, what a write does), not about
/// deserialization, so a hand-built registry keeps each test to exactly the entries it needs.
/// </summary>
public sealed class CampaignInstancesToolViewModelTests
{
    private static readonly Dnd5eContentSet Dnd5e = new();

    [Fact]
    public void The_instance_list_reflects_every_instance_the_campaign_holds()
    {
        var fixture = new Fixture(ResolvedGear("pack", "potion", "Mikstura leczenia"));
        fixture.Context.Instances.Add(new EntryAddress(ContentId.Create("pack"), ContentId.Create("potion")), "Fiolka Toma");

        var viewModel = fixture.CreateViewModel();

        var row = Assert.Single(viewModel.Instances);
        Assert.Equal("Fiolka Toma", row.DisplayName);
        Assert.False(row.HasMessage);
    }

    [Fact]
    public void An_unnamed_instance_falls_back_to_the_entry_it_points_at()
    {
        var fixture = new Fixture(ResolvedGear("pack", "potion", "Mikstura leczenia"));
        fixture.Context.Instances.Add(new EntryAddress(ContentId.Create("pack"), ContentId.Create("potion")), label: null);

        var viewModel = fixture.CreateViewModel();

        Assert.Equal("Mikstura leczenia", Assert.Single(viewModel.Instances).DisplayName);
    }

    [Fact]
    public void An_instance_pointing_at_a_missing_pack_stays_on_the_list_and_is_marked()
    {
        var fixture = new Fixture();
        fixture.Context.Instances.Add(new EntryAddress(ContentId.Create("ghost-pack"), ContentId.Create("ghost-entry")), label: null);

        var viewModel = fixture.CreateViewModel();

        var row = Assert.Single(viewModel.Instances);
        Assert.True(row.HasMessage);
        Assert.Contains("ghost-pack", row.Message);
    }

    [Fact]
    public async Task Adding_a_selected_entry_creates_an_instance_and_saves_through_the_session()
    {
        var fixture = new Fixture(ResolvedGear("pack", "potion", "Mikstura leczenia"));
        var viewModel = fixture.CreateViewModel();
        var option = Assert.Single(viewModel.AddableEntries);

        viewModel.SelectedToAdd = option;
        await viewModel.AddCommand.ExecuteAsync();

        Assert.Equal(1, fixture.Repository.SaveCount);
        var instance = Assert.Single(fixture.Context.Instances.All);
        Assert.Equal(option.Address, instance.Source);
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
    public void Addable_entries_exclude_unresolved_entries_and_entries_from_another_content_set()
    {
        var foreignSet = ContentId.Create("other-set");
        var foreignReference = new ContentTypeReference(foreignSet, ContentId.Create("thing"));
        var foreignEntry = new Entry(ContentId.Create("widget"), "Cudzy wpis", foreignReference, 1, ContentValues.Empty);
        var foreignRegistered = RegisteredEntry.CreateResolved(
            new EntryAddress(ContentId.Create("pack"), ContentId.Create("widget")),
            foreignEntry,
            new ContentTypeDescriptor(foreignReference, "Cokolwiek", 1));

        var unresolvedEntry = new Entry(
            ContentId.Create("broken"), "Zepsuty wpis", new ContentTypeReference(Dnd5e.Id, ContentId.Create("gear")), 1, ContentValues.Empty);
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
        var fixture = new Fixture(ResolvedMonster("pack", "goblin", "Goblin", hp: 7));
        fixture.Context.Instances.Add(new EntryAddress(ContentId.Create("pack"), ContentId.Create("goblin")), label: null);

        var row = Assert.Single(fixture.CreateViewModel().Instances);
        Assert.True(row.CanEditHitPoints);

        row.CurrentHp = 3;
        await row.SaveHitPointsCommand.ExecuteAsync();

        var patch = Assert.Single(fixture.Context.Instances.All).Patch;
        Assert.False(patch.IsEmpty);

        // A narrow shape naming only the changed field plus one field the entry itself always
        // carries - reading the patch through it proves the patch has the first and lacks the
        // second, without needing every one of Monster's twenty-one properties to deserialize it.
        var shape = patch.Read<SparsePatchShape>();
        Assert.Equal(3, shape.CurrentHp);
        Assert.Null(shape.Actions);
    }

    [Fact]
    public async Task After_saving_hit_points_the_merged_values_show_the_new_current_hp_and_the_entrys_max_is_untouched()
    {
        var fixture = new Fixture(ResolvedMonster("pack", "goblin", "Goblin", hp: 7));
        var address = new EntryAddress(ContentId.Create("pack"), ContentId.Create("goblin"));
        fixture.Context.Instances.Add(address, label: null);

        var row = Assert.Single(fixture.CreateViewModel().Instances);
        row.CurrentHp = 3;
        await row.SaveHitPointsCommand.ExecuteAsync();

        var instance = Assert.Single(fixture.Context.Instances.All);
        var resolved = fixture.Context.Resolver.Resolve(instance);
        var monster = resolved.Values!.Read<Monster>();

        Assert.Equal(3, monster.CurrentHp);
        Assert.Equal(7, monster.Hp);
    }

    [Fact]
    public async Task Setting_current_hit_points_back_to_the_entrys_own_value_leaves_the_patch_empty()
    {
        var fixture = new Fixture(ResolvedMonster("pack", "goblin", "Goblin", hp: 7));
        var address = new EntryAddress(ContentId.Create("pack"), ContentId.Create("goblin"));
        fixture.Context.Instances.Add(address, label: null);

        var firstRow = Assert.Single(fixture.CreateViewModel().Instances);
        firstRow.CurrentHp = 3;
        await firstRow.SaveHitPointsCommand.ExecuteAsync();

        var instanceId = Assert.Single(fixture.Context.Instances.All).Id;
        Assert.False(fixture.Context.Instances.Find(instanceId)!.Patch.IsEmpty);

        // The entry never declares a current hp of its own, so clearing the customization back to
        // "nothing" is what "equal to the entry" means here.
        var secondRow = Assert.Single(fixture.CreateViewModel().Instances);
        Assert.Equal(3, secondRow.CurrentHp);
        secondRow.CurrentHp = null;
        await secondRow.SaveHitPointsCommand.ExecuteAsync();

        Assert.True(fixture.Context.Instances.Find(instanceId)!.Patch.IsEmpty);
    }

    [Fact]
    public void A_gear_instance_row_cannot_edit_hit_points()
    {
        var fixture = new Fixture(ResolvedGear("pack", "potion", "Mikstura leczenia"));
        fixture.Context.Instances.Add(new EntryAddress(ContentId.Create("pack"), ContentId.Create("potion")), label: null);

        var row = Assert.Single(fixture.CreateViewModel().Instances);

        Assert.False(row.CanEditHitPoints);
        Assert.False(row.SaveHitPointsCommand.CanExecute(null));
    }

    [Fact]
    public void An_unresolved_instance_row_cannot_edit_hit_points_and_stays_on_the_list()
    {
        var fixture = new Fixture();
        fixture.Context.Instances.Add(new EntryAddress(ContentId.Create("ghost-pack"), ContentId.Create("ghost-entry")), label: null);

        var row = Assert.Single(fixture.CreateViewModel().Instances);

        Assert.True(row.HasMessage);
        Assert.False(row.CanEditHitPoints);
        Assert.False(row.SaveHitPointsCommand.CanExecute(null));
    }

    [Fact]
    public async Task Removing_an_instance_takes_it_off_the_list_and_saves_through_the_session()
    {
        var fixture = new Fixture(ResolvedGear("pack", "potion", "Mikstura leczenia"));
        fixture.Context.Instances.Add(new EntryAddress(ContentId.Create("pack"), ContentId.Create("potion")), label: null);

        var viewModel = fixture.CreateViewModel();
        var row = Assert.Single(viewModel.Instances);

        await row.RemoveCommand.ExecuteAsync();

        Assert.Equal(1, fixture.Repository.SaveCount);
        Assert.Empty(viewModel.Instances);
        Assert.Empty(fixture.Context.Instances.All);
    }

    [Fact]
    public void A_rows_commands_are_inactive_after_dispose()
    {
        var fixture = new Fixture(ResolvedMonster("pack", "goblin", "Goblin", hp: 7));
        fixture.Context.Instances.Add(new EntryAddress(ContentId.Create("pack"), ContentId.Create("goblin")), label: null);

        var row = Assert.Single(fixture.CreateViewModel().Instances);
        Assert.True(row.SaveHitPointsCommand.CanExecute(null));
        Assert.True(row.RemoveCommand.CanExecute(null));

        row.Dispose();

        Assert.False(row.SaveHitPointsCommand.CanExecute(null));
        Assert.False(row.RemoveCommand.CanExecute(null));
    }

    [Fact]
    public void Dispose_unsubscribes_so_a_later_instance_change_no_longer_refreshes_the_list()
    {
        var fixture = new Fixture();
        var viewModel = fixture.CreateViewModel();
        Assert.Empty(viewModel.Instances);

        viewModel.Dispose();
        fixture.Context.Instances.Add(new EntryAddress(ContentId.Create("pack"), ContentId.Create("potion")), label: null);

        Assert.Empty(viewModel.Instances);
    }

    private static RegisteredEntry ResolvedGear(string packId, string entryId, string name)
    {
        var reference = new ContentTypeReference(Dnd5e.Id, ContentId.Create("gear"));
        var entry = new Entry(ContentId.Create(entryId), name, reference, 1, ContentValues.From(new Gear { Rarity = "Pospolity" }));

        Assert.True(Dnd5e.TryGet(reference, out var descriptor));

        return RegisteredEntry.CreateResolved(new EntryAddress(ContentId.Create(packId), ContentId.Create(entryId)), entry, descriptor);
    }

    private static RegisteredEntry ResolvedMonster(string packId, string entryId, string name, int hp)
    {
        var reference = new ContentTypeReference(Dnd5e.Id, ContentId.Create("monster"));
        var monster = new Monster
        {
            Size = "Mały",
            Type = "goblinoid",
            Alignment = "chaotyczne zło",
            Ac = 15,
            Hp = hp,
            Speed = "9 m",
            Str = 8,
            Dex = 14,
            Con = 10,
            Int = 10,
            Wis = 8,
            Cha = 8,
            Senses = "wzrok w ciemności 18 m",
            Challenge = "1/4",
            Actions = "Tasak.",
        };
        var entry = new Entry(ContentId.Create(entryId), name, reference, 1, ContentValues.From(monster));

        Assert.True(Dnd5e.TryGet(reference, out var descriptor));

        return RegisteredEntry.CreateResolved(new EntryAddress(ContentId.Create(packId), ContentId.Create(entryId)), entry, descriptor);
    }

    /// <summary>
    /// Two of <see cref="Monster"/>'s properties, both optional here regardless of how they are
    /// declared on <see cref="Monster"/> itself - just enough to read a sparse patch without needing
    /// every required property present to deserialize it.
    /// </summary>
    private sealed record SparsePatchShape
    {
        public string? Actions { get; init; }

        public int? CurrentHp { get; init; }
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

            var campaign = Campaign.Create(CampaignName.Create("Testowa"), new DataBlockRegistry(), TimeProvider.System);
            var session = new CampaignSession(campaign, Repository);

            Context = new CampaignToolContext(session, Registry, Dnd5e);
        }

        public ContentRegistry Registry { get; }

        public InMemoryCampaignRepository Repository { get; }

        public CampaignToolContext Context { get; }

        public CampaignInstancesToolViewModel CreateViewModel() => new(Context, Dnd5e.Id);
    }
}
