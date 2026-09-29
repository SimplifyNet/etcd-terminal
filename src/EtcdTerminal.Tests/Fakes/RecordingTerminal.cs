using System.Text;
using EtcdTerminal.Terminal;

namespace EtcdTerminal.Tests.Fakes;

/// <summary>
/// Layout-recording terminal: configurable dimensions, cursor advancement by
/// display cells, recorded write positions and captured tables. Style markers
/// (`<name>`) cost no cells. Positions are recorded faithfully, even invalid
/// ones, so tests can assert production code never issues them.
/// </summary>
public sealed class RecordingTerminal : ITerminal
{
	public StringBuilder Output { get; } = new();

	public Queue<ConsoleKeyInfo> Keys { get; } = new();

	public List<(int Top, int Left, string Text)> Writes { get; } = [];

	public List<(int Left, int Top)> CursorSets { get; } = [];

	public int WindowWidth { get; set; } = 80;

	public int WindowHeight { get; set; } = 24;

	public int CursorLeft { get; set; }

	public int CursorTop { get; set; }

	public string Subtle => "<subtle>";

	public string Accent => "<accent>";

	public string Reset => "</>";

	public bool KeyAvailable => Keys.Count > 0;

	public string SelectionPointer => "  > ";

	public string Indent => "    ";

	public void Write(string text)
	{
		Writes.Add((CursorTop, CursorLeft, text));
		Output.Append(text);

		Advance(text);
	}

	public void Write(string text, TerminalColor color) => Write(text);

	public void WriteLine(string text)
	{
		Write(text);
		WriteLine();
	}

	public void WriteLine(string text, TerminalColor color) => WriteLine(text);

	public void WriteLine()
	{
		Writes.Add((CursorTop, CursorLeft, "\n"));
		Output.AppendLine();

		CursorLeft = 0;
		CursorTop++;
	}

	public void WriteIndentedLine(string text) => WriteLine(Indent + text);

	public void WriteIndentedLine(string text, TerminalColor color) => WriteIndentedLine(text);

	public void Clear() => Output.Append("[clear]");

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

	public ConsoleKeyInfo ReadKey()
	{
		if (Keys.Count == 0)
			throw new InvalidOperationException("No more keys");

		return Keys.Dequeue();
	}

	public void WriteException(Exception ex) => Output.Append("[exception:" + ex.Message + "]");

	public void SetCursorVisible(bool visible)
	{
	}

	public void OnInterrupt(Action handler)
	{
	}

	public void Press(params ConsoleKey[] keys)
	{
		foreach (var key in keys)
			Keys.Enqueue(new ConsoleKeyInfo('\0', key, false, false, false));
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
