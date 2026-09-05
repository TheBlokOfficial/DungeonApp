using System;
using System.Collections.Generic;
using DungeonApp.Core.Events;

namespace DungeonApp.Core.Tests.Events;

public sealed class CampaignEventsTests
{
    private sealed record Announced(int Value) : ICampaignEvent;

    private sealed record Other : ICampaignEvent;

    private readonly CampaignEvents _events = new();

    [Fact]
    public void Delivers_to_everyone_listening_for_that_fact()
    {
        var heard = new List<int>();

        _events.Subscribe<Announced>(announcement => heard.Add(announcement.Value));
        _events.Subscribe<Announced>(announcement => heard.Add(announcement.Value * 10));

        _events.Publish(new Announced(3));

        Assert.Equal([3, 30], heard);
    }

    /// <summary>
    /// Subscription order is activation order, and activation order is settled per campaign. Without
    /// this the same save could resolve differently on two runs.
    /// </summary>
    [Fact]
    public void Runs_handlers_in_the_order_they_subscribed()
    {
        var order = new List<string>();

        _events.Subscribe<Announced>(_ => order.Add("first"));
        _events.Subscribe<Announced>(_ => order.Add("second"));
        _events.Subscribe<Announced>(_ => order.Add("third"));

        _events.Publish(new Announced(1));

        Assert.Equal(["first", "second", "third"], order);
    }

    [Fact]
    public void Ignores_an_announcement_nobody_listens_for()
        => _events.Publish(new Other());

    /// <summary>An event is what it says it is; nothing receives it by way of a base type.</summary>
    [Fact]
    public void Matches_the_exact_type_only()
    {
        var heard = 0;

        _events.Subscribe<Announced>(_ => heard++);
        _events.Publish(new Other());

        Assert.Equal(0, heard);
    }

    [Fact]
    public void Delivers_synchronously_before_returning()
    {
        var heard = false;

        _events.Subscribe<Announced>(_ => heard = true);
        _events.Publish(new Announced(1));

        Assert.True(heard);
    }

    /// <summary>
    /// A handler that reacts by announcing something itself is the normal case - the scheduler does
    /// exactly that when time passes.
    /// </summary>
    [Fact]
    public void Allows_a_handler_to_announce_in_turn()
    {
        var heard = new List<string>();

        _events.Subscribe<Announced>(_ =>
        {
            heard.Add("announced");
            _events.Publish(new Other());
        });
        _events.Subscribe<Other>(_ => heard.Add("other"));

        _events.Publish(new Announced(1));

        Assert.Equal(["announced", "other"], heard);
    }

    /// <summary>
    /// Two modules answering each other must crash legibly rather than freeze the window in the
    /// middle of a session.
    /// </summary>
    [Fact]
    public void Stops_a_runaway_cascade_with_a_named_error()
    {
        _events.Subscribe<Announced>(announcement => _events.Publish(new Announced(announcement.Value + 1)));

        Assert.Throws<EventCascadeException>(() => _events.Publish(new Announced(0)));
    }

    /// <summary>
    /// The budget belongs to one command. A long session of separate commands, each modest, must not
    /// accumulate its way into a false alarm.
    /// </summary>
    [Fact]
    public void Gives_every_command_a_fresh_budget()
    {
        var heard = 0;
        _events.Subscribe<Announced>(_ => heard++);

        for (var command = 0; command < CampaignEvents.MaxEventsPerCommand + 10; command++)
        {
            _events.Publish(new Announced(command));
        }

        Assert.Equal(CampaignEvents.MaxEventsPerCommand + 10, heard);
    }

    /// <summary>A cascade that was stopped must leave the bus usable, not wedged at depth.</summary>
    [Fact]
    public void Recovers_after_a_cascade_was_stopped()
    {
        var runaway = true;
        _events.Subscribe<Announced>(announcement =>
        {
            if (runaway)
            {
                _events.Publish(new Announced(announcement.Value + 1));
            }
        });

        Assert.Throws<EventCascadeException>(() => _events.Publish(new Announced(0)));

        runaway = false;
        _events.Publish(new Announced(0));
    }

    [Fact]
    public void Disposing_one_of_two_equal_handler_registrations_removes_only_its_own_registration()
    {
        var heard = 0;
        Action<Announced> handler = _ => heard++;

        var first = _events.Subscribe(handler);
        _events.Subscribe(handler);

        first.Dispose();
        _events.Publish(new Announced(1));

        Assert.Equal(1, heard);
    }

    [Fact]
    public void Disposing_the_second_of_three_registrations_removes_its_exact_handler()
    {
        var heard = new List<string>();
        Action<Announced> handler = _ => heard.Add("A");

        var first = _events.Subscribe(handler);
        _events.Subscribe<Announced>(_ => heard.Add("B"));
        var second = _events.Subscribe(handler);

        second.Dispose();
        second.Dispose();
        _events.Publish(new Announced(1));

        Assert.Equal(["A", "B"], heard);
        first.Dispose();
    }

    [Fact]
    public void Disposing_a_registration_during_a_publish_keeps_the_current_snapshot_intact()
    {
        var heard = 0;
        IDisposable? second = null;
        _events.Subscribe<Announced>(_ => second!.Dispose());
        second = _events.Subscribe<Announced>(_ => heard++);

        _events.Publish(new Announced(1));
        _events.Publish(new Announced(2));

        Assert.Equal(1, heard);
    }
}
