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
