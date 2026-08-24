namespace DungeonApp.Domain.Campaigns;

/// <summary>
/// Stable identifier of a campaign module. It is part of a persisted campaign contract.
/// </summary>
public sealed record ModuleId
{
    public ModuleId(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Module id cannot be empty.", nameof(value));
        }

        Value = value.Trim();
    }

    public string Value { get; }

    public override string ToString() => Value;
}
