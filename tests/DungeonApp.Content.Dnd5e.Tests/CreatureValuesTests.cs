using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DungeonApp.Core.Entries;
using DungeonApp.Core.Entries.Entities;
using DungeonApp.Core.State;
using DungeonApp.Testing;

namespace DungeonApp.Content.Dnd5e.Tests;

/// <summary>
/// A creature's live numbers: the bases read from the entry, and the book's arithmetic of current
/// hit points - "-" stops at 0 and reports the rest, "+" stops at the maximum, "=" sets anything.
/// </summary>
public sealed class CreatureValuesTests
{
    private static readonly CreatureValues.HitPointsRule HitPoints = new();

    [Theory]
    [InlineData(8, 5, 3, 0)]
    [InlineData(3, 8, 0, 5)]
    [InlineData(3, 3, 0, 0)]
    [InlineData(0, 4, 0, 4)]
    [InlineData(-2, 4, -2, 4)]
    [InlineData(20, 5, 15, 0)]
    public void Subtracting_stops_at_zero_and_reports_the_excess(int current, int amount, int value, int excess) =>
        Assert.Equal(new TrackedChangeResult(value, excess), HitPoints.Apply(current, 13, new NumberChange(NumberChangeKind.Subtract, amount)));

    [Theory]
    [InlineData(5, 3, 8)]
    [InlineData(10, 5, 13)]
    [InlineData(13, 1, 13)]
    [InlineData(20, 4, 20)]
    [InlineData(-3, 2, -1)]
    public void Adding_stops_at_the_maximum(int current, int amount, int value) =>
        Assert.Equal(new TrackedChangeResult(value, 0), HitPoints.Apply(current, 13, new NumberChange(NumberChangeKind.Add, amount)));

    [Theory]
    [InlineData(30)]
    [InlineData(0)]
    [InlineData(-5)]
    [InlineData(7)]
    public void Setting_takes_any_value_above_the_maximum_or_below_zero(int amount) =>
        Assert.Equal(new TrackedChangeResult(amount, 0), HitPoints.Apply(4, 13, new NumberChange(NumberChangeKind.Set, amount)));

    [Fact]
    public void Untouched_hit_points_are_the_maximum() =>
        Assert.Equal(13, HitPoints.DefaultCurrent(13));

    [Fact]
    public async Task A_goblin_entity_shows_its_entry_values_and_full_hit_points_until_the_gm_changes_them()
    {
        var (system, registry) = await LoadAsync();
        var goblin = registry.Entries.Single(entry => entry.Address.Entry.Value == "goblin");
        var entity = new CampaignEntity { Id = EntityId.New(), Source = goblin.Address, Number = 1, Patch = ContentValues.Empty };
        var resolver = new EntityResolver(registry, system);
        var catalog = system.GetWorldCatalogSource().Values;

        var live = EntityLivePart.Read(resolver.Resolve(entity), catalog);

        Assert.Equal(
            ["KP 15", "PW maks. 7", "SIŁ 8", "ZRĘ 14", "KON 10", "INT 10", "MDR 8", "CHA 8"],
            live.Values.Select(value => $"{value.Label} {value.Shown}"));
        var hp = Assert.Single(live.Tracked);
        Assert.Equal((CreatureValues.HitPointsKey, 7, 7), (hp.Key, hp.Current, hp.Maximum));

        var blow = new NumberChange(NumberChangeKind.Subtract, 10);
        Assert.Equal("7 → 0 · 3", hp.Preview(blow));

        var changed = CampaignStateSnapshot.Empty.Apply(live.Change(CreatureValues.HitPointsKey, blow))
            .Get(EntitiesModel.Declaration)[entity.Id.ToString()];
        Assert.Equal(0, EntityLivePart.Read(resolver.Resolve(changed), catalog).Tracked.Single().Current);
    }

    [Fact]
    public async Task An_item_has_no_live_numbers()
    {
        var (system, registry) = await LoadAsync();
        var potion = registry.Entries.Single(entry => entry.Address.Entry.Value == "healing-potion");

        Assert.Null(system.For(potion.Entry.Type));
    }

    private static async Task<(Dnd5eSystem System, ContentRegistry Registry)> LoadAsync()
    {
        var system = new Dnd5eSystem([Path.Combine(Path.GetTempPath(), $"dnd5e-values-tests-packs-{Guid.NewGuid():N}")]);
        var registry = await new ContentPackLoader(RepositoryRoot.PackFixtures, system).LoadAsync(CancellationToken.None);
        return (system, registry);
    }
}
