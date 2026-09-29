namespace EtcdTerminal.Presentation;

/// <summary>
/// Footer content. Every field keeps its own identity and role so that
/// Infrastructure can shorten the footer in priority order without the
/// component knowing the available width. Assembly of separators belongs to
/// Infrastructure; the component owns the literal text of each field.
/// </summary>
public sealed record StatusBarModel
{
	/// <summary>
	/// Keyboard hints shown on the left side. Dropped first when space is short.
	/// </summary>
	public IReadOnlyList<StyledText> Hints { get; init; } = [];

	/// <summary>
	/// Name of the active connection, or null when there is no session.
	/// </summary>
	public StyledText? Name { get; init; }

	/// <summary>
	/// Endpoint of the active connection, or null when unknown.
	/// </summary>
	public StyledText? Connection { get; init; }

	/// <summary>
	/// Authenticated user, or null when the connection is unauthenticated.
	/// </summary>
	public StyledText? Username { get; init; }

	/// <summary>
	/// Application version, including its label. The single owner of version
	/// formatting is the component that builds this model.
	/// </summary>
	public StyledText Version { get; init; } = new("v");
}
