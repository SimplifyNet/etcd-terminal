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

	public const string SelectionPointer = "  ❯ ";
}
