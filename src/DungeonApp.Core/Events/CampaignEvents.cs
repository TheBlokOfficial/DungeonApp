using System;
using System.Collections.Generic;

namespace DungeonApp.Core.Events;

/// <summary>
/// How modules inside one campaign hear about facts. Kept as small as it can be, because a general
/// purpose bus is where determinism and explainability usually go to die.
/// <para>
/// Four guarantees, each one deliberate:
/// </para>
/// <list type="bullet">
/// <item>Synchronous only. One command is handled to the end before control returns - no queue, no
/// threads, no ordering that depends on when something happened to be scheduled.</item>
/// <item>One instance per campaign. Never static, never global; two open campaigns cannot hear each
/// other.</item>
/// <item>Deterministic order. Handlers run in subscription order, and modules subscribe while being
/// activated, so the order is the campaign's settled activation order. The same save produces the
/// same result tomorrow.</item>
/// <item>A capped cascade. A publish/subscribe loop surfaces as a named error rather than a hang.</item>
/// </list>
/// <para>
/// Matching is by exact type. An event is what it says it is, and no handler receives it by virtue
/// of a base type it was never told about.
/// </para>
/// </summary>
public sealed class CampaignEvents
{
    /// <summary>
    /// High enough that no honest rule reaches it, low enough to stop a loop while the GM is still
    /// looking at the screen.
    /// </summary>
    public const int MaxEventsPerCommand = 1000;

    private readonly Dictionary<Type, List<Delegate>> _handlers = [];

    private int _depth;
    private int _dispatched;

    public void Subscribe<TEvent>(Action<TEvent> handler) where TEvent : ICampaignEvent
    {
        ArgumentNullException.ThrowIfNull(handler);

        if (!_handlers.TryGetValue(typeof(TEvent), out var handlers))
        {
            _handlers[typeof(TEvent)] = handlers = [];
        }

        handlers.Add(handler);
    }

    public void Publish<TEvent>(TEvent announcement) where TEvent : ICampaignEvent
    {
        ArgumentNullException.ThrowIfNull(announcement);

        // The budget belongs to the outermost publish - that is the command the GM asked for.
        if (_depth == 0)
        {
            _dispatched = 0;
        }

        if (++_dispatched > MaxEventsPerCommand)
        {
            throw new EventCascadeException(
                $"One command produced more than {MaxEventsPerCommand} events. "
                + "Two modules are almost certainly answering each other.");
        }

        _depth++;

        try
        {
            if (!_handlers.TryGetValue(typeof(TEvent), out var handlers))
            {
                return;
            }

            // A copy, so a module that subscribes while handling does not join the round it is in
            // the middle of - and so the list can be added to safely during dispatch.
            foreach (var handler in handlers.ToArray())
            {
                ((Action<TEvent>)handler)(announcement);
            }
        }
        finally
        {
            _depth--;
        }
    }
}
