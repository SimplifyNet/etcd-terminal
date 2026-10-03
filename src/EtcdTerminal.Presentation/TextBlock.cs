namespace EtcdTerminal.Presentation;

/// <summary>
/// Free lines of styled runs: notices, hints, details and any other text that
/// is not a table or a heading.
/// </summary>
public sealed record TextBlock(IReadOnlyList<IReadOnlyList<StyledText>> Lines) : Block
{
	/// <summary>
	/// A single line built from the given runs.
	/// </summary>
	public static TextBlock Line(params IReadOnlyList<StyledText> spans) => new([spans]);

	/// <summary>
	/// An empty line used for spacing between blocks.
	/// </summary>
	public static TextBlock Blank() => new([[]]);
}
