using System;
using System.Collections.Generic;
using System.Linq;
using DungeonApp.Core.Events;
using DungeonApp.Core.Modules;
using DungeonApp.Core.Tests.Fakes;

namespace DungeonApp.Core.Tests.Modules;

public sealed class CampaignModulesTests
{
    /// <summary>The clock naming is incidental here; these tests are about the set itself.</summary>
    private static CampaignModules Activate(IEnumerable<ICampaignModule> modules)
        => CampaignModules.Activate(modules, new CampaignEvents());

    [Fact]
    public void Refuses_the_same_module_twice()
    {
        var exception = Assert.Throws<ModuleActivationException>(
            () => Activate([new StubModule("core.clock"), new StubModule("core.clock")]));

        Assert.Equal(ModuleActivationFailure.DuplicateModule, exception.Failure);
    }

    /// <summary>
    /// A scheduler without a clock is the case this exists for: refused when the campaign is
    /// assembled, not discovered when the GM advances time mid-session.
    /// </summary>
    [Fact]
    public void Refuses_a_module_whose_dependency_is_switched_off()
    {
        var exception = Assert.Throws<ModuleActivationException>(
            () => Activate([new StubModule("core.scheduler", "core.clock")]));

        Assert.Equal(ModuleActivationFailure.MissingDependency, exception.Failure);
    }

    [Fact]
    public void Refuses_a_dependency_loop_instead_of_overflowing()
    {
        var exception = Assert.Throws<ModuleActivationException>(() => Activate([
            new StubModule("a", "b"),
            new StubModule("b", "a")
        ]));

        Assert.Equal(ModuleActivationFailure.CircularDependency, exception.Failure);
    }

    /// <summary>
    /// Declared before requested: the caller listed the scheduler first, but the clock has to be
    /// activated first regardless.
    /// </summary>
    [Fact]
    public void Activates_dependencies_before_the_modules_that_declared_them()
    {
        var clock = new StubModule("core.clock");
        var scheduler = new StubModule("core.scheduler", "core.clock");

        var modules = Activate([scheduler, clock]);

        Assert.Equal(
            ["core.clock", "core.scheduler"],
            modules.Active.Select(module => module.Manifest.Id.Value));
        Assert.True(clock.ActivatedAt < scheduler.ActivatedAt);
    }

    [Fact]
    public void Activates_every_module_exactly_once()
    {
        var clock = new StubModule("core.clock");

        Activate([clock, new StubModule("core.scheduler", "core.clock")]);

        Assert.NotNull(clock.ActivatedAt);
        Assert.NotNull(clock.Context);
    }

    /// <summary>
    /// Registration completes before any activation runs, so a module can reach a declared
    /// dependency from inside OnActivated.
    /// </summary>
    [Fact]
    public void Lets_a_module_resolve_its_dependency_while_activating()
    {
        var clock = new StubModule("core.clock");
        var dependent = new DependentModule("core.scheduler", "core.clock");

        Activate([dependent, clock]);

        Assert.Same(clock, dependent.Resolved);
    }

    [Fact]
    public void Finds_a_switched_on_module_by_id()
    {
        var clock = new StubModule("core.clock");
        var modules = Activate([clock]);

        Assert.Same(clock, modules.Find(ModuleId.Create("core.clock")));
        Assert.True(modules.Contains(ModuleId.Create("core.clock")));
    }

    [Fact]
    public void Reports_a_module_that_is_not_switched_on_rather_than_inventing_one()
    {
        var modules = Activate([]);

        Assert.Null(modules.Find(ModuleId.Create("core.clock")));
        Assert.False(modules.TryGet<StubModule>(out _));
        Assert.Throws<System.InvalidOperationException>(() => modules.Get<StubModule>());
    }
}
