using System;

namespace DungeonApp.Library.Entries.Tests.Fakes;

/// <summary>A clock frozen at one moment. Enough for now; no testing package pulled in for it.</summary>
internal sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
{
    public override DateTimeOffset GetUtcNow() => now;
}
