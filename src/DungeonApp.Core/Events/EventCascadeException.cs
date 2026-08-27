using System;

namespace DungeonApp.Core.Events;

/// <summary>
/// One command produced more events than any sane rule could. Raised instead of letting a
/// publish/subscribe loop hang the application mid-session: at the table, a legible crash beats a
/// frozen window.
/// </summary>
public sealed class EventCascadeException(string message) : Exception(message);
