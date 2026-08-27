using System;

namespace DungeonApp.Core.Campaigns;

/// <summary>
/// A campaign's permanent identity. Deliberately separate from its name: the name is the GM's
/// editable label, while this is what saved state, content provenance and the desk layout key point
/// at. It has to survive every rename, so the two must never be the same value.
/// </summary>
public readonly record struct CampaignId(Guid Value)
{
    public static CampaignId New() => new(Guid.NewGuid());

    public override string ToString() => Value.ToString();
}
