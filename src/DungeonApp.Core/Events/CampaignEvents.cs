using System;
using System.Collections.Generic;
using System.Threading;

namespace DungeonApp.Core.Events;

/// <summary>
/// Jak części jednej kampanii słyszą o faktach. Mechanizm pozostaje mały, bo ogólna magistrala
/// szybko zaciera kolejność i przyczynę zmian.
/// <para>
/// Four guarantees, each one deliberate:
/// </para>
/// <list type="bullet">
/// <item>Synchronous only. One command is handled to the end before control returns - no queue, no
/// threads, no ordering that depends on when something happened to be scheduled.</item>
/// <item>One instance per campaign. Never static, never global; two open campaigns cannot hear each
/// other.</item>
/// <item>Deterministyczna kolejność. Handlery działają w kolejności subskrypcji.</item>
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

    private readonly Dictionary<Type, List<Subscription>> _handlers = [];

    private int _depth;
    private int _dispatched;

    public IDisposable Subscribe<TEvent>(Action<TEvent> handler) where TEvent : ICampaignEvent
    {
        ArgumentNullException.ThrowIfNull(handler);

        if (!_handlers.TryGetValue(typeof(TEvent), out var handlers))
        {
            _handlers[typeof(TEvent)] = handlers = [];
        }

        var subscription = new Subscription(handler);
        handlers.Add(subscription);
        return new SubscriptionHandle(() => handlers.Remove(subscription));
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
                $"Jedno polecenie wywołało więcej niż {MaxEventsPerCommand} zdarzeń. "
                + "Narzędzia prawdopodobnie odpowiadają na swoje własne zdarzenia.");
        }

        _depth++;

        try
        {
            if (!_handlers.TryGetValue(typeof(TEvent), out var handlers))
            {
                return;
            }

            // Migawka zachowuje porządek bieżącego ogłoszenia. Subskrypcja albo odpięcie w trakcie
            // obsługi wpływa dopiero na następne zdarzenie.
            foreach (var subscription in handlers.ToArray())
            {
                ((Action<TEvent>)subscription.Handler)(announcement);
            }
        }
        finally
        {
            _depth--;
        }
    }

    private sealed class Subscription(Delegate handler)
    {
        public Delegate Handler { get; } = handler;
    }

    private sealed class SubscriptionHandle(Action unsubscribe) : IDisposable
    {
        private Action? _unsubscribe = unsubscribe;

        public void Dispose()
        {
            var unsubscribe = Interlocked.Exchange(ref _unsubscribe, null);
            unsubscribe?.Invoke();
        }
    }
}
