namespace EtcdTerminal.App.Screens.Keys.Import;

/// <summary>
/// Why a JSON document could not be turned into entries: the raw parser error
/// is kept as <see cref="Detail"/> so the caller can localize the wording.
/// </summary>
public sealed record ImportParseFailure(ImportParseFailureKind Kind, string? Detail);
