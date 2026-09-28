using Spectre.Console;

namespace EtcdTerminal.Infrastructure.Terminal;

/// <summary>
/// Absolute cursor positioning for the pinned footer and for handing the cursor
/// back after a frame. Spectre's <c>SetPosition(column, line)</c> is emitted
/// verbatim as a CUP sequence, which is one based, while the non ANSI backend
/// assigns the two based System.Console coordinates; the two backends therefore
/// disagree by one row and column. The gap is compensated here instead of
/// writing escape sequences by hand.
/// </summary>
internal static class SpectreCursorPosition
{
	public static void MoveTo(IAnsiConsoleCursor cursor, bool ansiSupported, int column, int line) =>
		cursor.SetPosition(ansiSupported ? column + 1 : column, ansiSupported ? line + 1 : line);
}
