using System.Text;

namespace EtcdTerminal.App.Components;

public static class DisplayText
{
	private const string NewLineMark = "\u23CE";
	private const string TabMark = "\u2192";
	private const string ControlMark = "\uFFFD";

	/// <summary>
	/// Normalizes control content for display without applying any width
	/// budget. Callers that let Infrastructure own layout use this and let the
	/// renderer decide how much of the result fits.
	/// </summary>
	public static string Sanitize(string value)
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
