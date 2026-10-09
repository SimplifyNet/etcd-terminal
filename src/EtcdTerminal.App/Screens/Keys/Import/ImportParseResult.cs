namespace EtcdTerminal.App.Screens.Keys.Import;

/// <summary>
/// The parsed entries, or the reason the document produced none. Exactly one of
/// the two is meaningful: a failure means <see cref="Entries"/> is empty.
/// </summary>
public sealed record ImportParseResult(IReadOnlyList<KeyValuePair<string, string>> Entries, ImportParseFailure? Failure);
