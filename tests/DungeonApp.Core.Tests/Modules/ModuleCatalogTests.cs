using System;
using System.Linq;
using DungeonApp.Core.Modules;
using DungeonApp.Core.Modules.Clock;
using DungeonApp.Core.Modules.Party;
using DungeonApp.Core.Modules.Scheduler;

namespace DungeonApp.Core.Tests.Modules;

public sealed class ModuleCatalogTests
{
    private readonly ModuleCatalog _catalog = new ModuleCatalog()
        .Register(ClockModule.Id, () => new ClockModule())
        .Register(SchedulerModule.Id, () => new SchedulerModule())
        .Register(PartyModule.Id, () => new PartyModule());

    /// <summary>What the GM is offered, in the order the composition root meant.</summary>
    [Fact]
    public void Lists_what_this_build_can_make_in_registration_order()
        => Assert.Equal(
            [ClockModule.Id, SchedulerModule.Id, PartyModule.Id],
            _catalog.Manifests.Select(manifest => manifest.Id));

    [Fact]
    public void Describes_a_module_without_a_campaign_to_put_it_in()
        => Assert.Equal("Zegar kampanii", _catalog.Describe(ClockModule.Id).DisplayName);

    [Fact]
    public void Refuses_to_describe_a_module_this_build_does_not_have()
        => Assert.Throws<InvalidOperationException>(
            () => _catalog.Describe(ModuleId.Create("core.nieznany")));

    /// <summary>
    /// A GM ticking the scheduler is asking for something that cannot work without a clock, not
    /// making a statement about clocks.
    /// </summary>
    [Fact]
    public void Adds_what_the_chosen_modules_need()
        => Assert.Equal(
            [ClockModule.Id, SchedulerModule.Id],
            _catalog.WithRequirements([SchedulerModule.Id]));

    [Fact]
    public void Settles_on_the_same_order_whatever_order_it_is_given()
        => Assert.Equal(
            _catalog.WithRequirements([PartyModule.Id, SchedulerModule.Id]),
            _catalog.WithRequirements([SchedulerModule.Id, PartyModule.Id]));

    [Fact]
    public void Keeps_a_module_once_even_when_it_is_both_chosen_and_needed()
        => Assert.Equal(
            [ClockModule.Id, SchedulerModule.Id],
            _catalog.WithRequirements([ClockModule.Id, SchedulerModule.Id, ClockModule.Id]));

    [Fact]
    public void Closes_an_empty_choice_to_nothing()
        => Assert.Empty(_catalog.WithRequirements([]));

    /// <summary>
    /// A factory filed under the wrong id would write one module's state under another's name. It
    /// is caught at startup rather than the first time a campaign is saved.
    /// </summary>
    [Fact]
    public void Refuses_a_factory_filed_under_the_wrong_name()
        => Assert.Throws<ArgumentException>(
            () => _catalog.Register(ModuleId.Create("core.cokolwiek"), () => new ClockModule()));

    [Fact]
    public void Refuses_the_same_module_twice()
        => Assert.Throws<ArgumentException>(
            () => _catalog.Register(ClockModule.Id, () => new ClockModule()));
}
