using System.Text;
using EtcdTerminal.Presentation.Terminal;

namespace EtcdTerminal.Tests.Fakes;

/// <summary>
/// Cursor and dimension stub: configurable window size, cursor advancement by
/// display cells and recorded cursor moves. Style markers (`<name>`) cost no
/// cells. Positions are recorded faithfully, even invalid ones, so tests can
/// assert production code never issues them.
/// </summary>
public sealed class RecordingTerminal : ITerminal
{
	public List<(int Left, int Top)> CursorSets { get; } = [];

	public int WindowWidth { get; set; } = 80;

	public int WindowHeight { get; set; } = 24;

	public int CursorLeft { get; set; }

	public int CursorTop { get; set; }

	public string Subtle => "<subtle>";

	public string Accent => "<accent>";

	public string Reset => "</>";

	public bool KeyAvailable => false;

	public string SelectionPointer => "  > ";

	public string Indent => "    ";

	public void Write(string text) => Advance(text);

	public void Write(string text, TerminalColor color) => Write(text);

	public void WriteLine(string text)
	{
		Write(text);
		WriteLine();
	}

	public void WriteLine(string text, TerminalColor color) => WriteLine(text);

	public void WriteLine()
	{
		CursorLeft = 0;
		CursorTop++;
	}

	public void WriteIndentedLine(string text) => WriteLine(Indent + text);

	public void WriteIndentedLine(string text, TerminalColor color) => WriteIndentedLine(text);

	public void Clear()
	{
	}

	public void SetCursorPosition(int left, int top)
	{
		CursorSets.Add((left, top));

		CursorLeft = left;
		CursorTop = top;
	}

	public void ResetBackground()
	{
	}

	public void Flush()
	{
	}

	public void Initialize()
	{
	}

	public void Shutdown()
	{
	}

	public ConsoleKeyInfo ReadKey() => throw new NotSupportedException();

	public void WriteException(Exception ex)
	{
	}

	public void SetCursorVisible(bool visible)
	{
	}

	public void OnInterrupt(Action handler)
	{
	}

	private void Advance(string text)
	{
		foreach (var rune in StripMarkers(text).EnumerateRunes())
		{
			if (rune.Value == '\n')
			{
				CursorLeft = 0;
				CursorTop++;
			}
			else if (rune.Value == '\r')
			{
				CursorLeft = 0;
			}
			else
			{
				CursorLeft += DisplayCells.Width(rune);
			}
		}
	}

	private static string StripMarkers(string text)
	{
		var clean = new StringBuilder(text.Length);
		var i = 0;

		while (i < text.Length)
		{
			if (text[i] == '<')
			{
				var end = text.IndexOf('>', i);

				if (end >= 0)
				{
					i = end + 1;
					continue;
				}
			}

			clean.Append(text[i]);
			i++;
		}

		return clean.ToString();
	}
}
