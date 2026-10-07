namespace EtcdTerminal.Infrastructure.Terminal;

/// <summary>
/// The left margin and the selection pointer every content row had before the
/// migration: text starts on the fourth column and the pointer of the current
/// row sits on the second one, so the text of a selected row lines up with the
/// text of every other row.
/// </summary>
public static class ContentIndent
{
	public const int Columns = 4;

	public const string Text = "    ";

	// A band panel — the status bar, the key filter, the pagination — hangs on
	// the second column: the margin the status bar has always had.
	public const int BandColumns = 2;

	// A marker (the selection pointer, a spinner glyph) hangs on the second
	// column so the text after it starts on the fourth, like any other row.
	public const string Marker = "  ";

	public const string SelectionPointer = "  ❯ ";
}
