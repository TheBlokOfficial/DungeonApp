using System;

namespace DungeonApp.Core;

/// <summary>
/// A refusal the GM is meant to read: an operation that the rules of the campaign do not allow.
/// <para>
/// Unlike the store's failures, this carries no typed reason. The set of rules is open and grows
/// with every module, and only the module knows why it said no - so the message is written for the
/// GM rather than mapped by the UI. Diagnostics that are never shown stay in English; a sentence
/// meant for the person at the table is written in theirs.
/// </para>
/// </summary>
public sealed class CampaignRuleException(string message) : Exception(message);
