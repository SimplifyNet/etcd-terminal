using System.Text;
using EtcdTerminal.Terminal;

namespace EtcdTerminal.App.Components;

/// <summary>
/// Single-line plain-text previews for keys and values. Control content is
/// normalized for display only; truncation keeps the ellipsis inside the
/// width budget and never splits a rune. Stored values are never modified.
/// </summary>
public static class ValuePreview
{
	private const string Ellipsis = "\u2026";
	private const string NewLineMark = "\u23CE";
	private const string TabMark = "\u2192";
	private const string ControlMark = "\uFFFD";

	/// <summary>
	/// Normalizes control content for display without applying any width budget.
	/// Callers that let Infrastructure own layout use this and let the renderer
	/// decide how much of the result fits.
	/// </summary>
	public static string Sanitize(string value) => Flatten(value);

	public static string Preview(string value, int maxWidth)
	{
		if (maxWidth <= 0)
			return string.Empty;

		var singleLine = Flatten(value);

		if (DisplayCells.Width(singleLine) <= maxWidth)
			return singleLine;

		var budget = maxWidth - DisplayCells.Width(Ellipsis);
		var width = 0;
		var length = 0;

		foreach (var rune in singleLine.EnumerateRunes())
		{
			var runeWidth = DisplayCells.Width(rune);

			if (width + runeWidth > budget)
				break;

			width += runeWidth;
			length += rune.Utf16SequenceLength;
		}

		return singleLine[..length] + Ellipsis;
	}

	private static string Flatten(string value)
	{
		var normalized = value.Replace("\r\n", "\n");
		var singleLine = new StringBuilder(normalized.Length);

		foreach (var rune in normalized.EnumerateRunes())
		{
			if (rune.Value is '\r' or '\n')
				singleLine.Append(NewLineMark);
			else if (rune.Value == '\t')
				singleLine.Append(TabMark);
			else if (Rune.IsControl(rune))
				singleLine.Append(ControlMark);
			else
				singleLine.Append(rune.ToString());
		}

		return singleLine.ToString();
	}
}
