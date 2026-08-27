using System;
using System.Diagnostics.CodeAnalysis;

namespace DungeonApp.Core.Campaigns;

/// <summary>Why a candidate campaign name was rejected.</summary>
public enum CampaignNameError
{
    None = 0,
    Empty,
    TooLong
}

/// <summary>
/// A validated campaign name. It exists as a type rather than a bare string so the rule has one
/// home: the UI validates by asking <see cref="Validate"/> instead of restating the limits in a
/// ViewModel, which is exactly the kind of duplication that lets the two drift apart.
/// </summary>
public sealed record CampaignName
{
    public const int MaxLength = 100;

    private CampaignName(string value) => Value = value;

    public string Value { get; }

    /// <summary>
    /// Cheap enough to call on every keystroke, which is the point: the form can show the reason
    /// before the GM commits, without the Core throwing anything.
    /// </summary>
    public static CampaignNameError Validate(string? candidate)
    {
        var trimmed = candidate?.Trim();

        if (string.IsNullOrEmpty(trimmed))
        {
            return CampaignNameError.Empty;
        }

        return trimmed.Length > MaxLength ? CampaignNameError.TooLong : CampaignNameError.None;
    }

    public static bool TryCreate(string? candidate, [NotNullWhen(true)] out CampaignName? name)
    {
        name = Validate(candidate) is CampaignNameError.None
            ? new CampaignName(candidate!.Trim())
            : null;

        return name is not null;
    }

    /// <summary>For callers that have already validated. An invalid name here is a programmer error.</summary>
    public static CampaignName Create(string? candidate) =>
        TryCreate(candidate, out var name)
            ? name
            : throw new ArgumentException(
                $"Invalid campaign name ({Validate(candidate)}).",
                nameof(candidate));

    public override string ToString() => Value;
}
