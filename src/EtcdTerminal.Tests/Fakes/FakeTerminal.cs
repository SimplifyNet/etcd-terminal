using System.Text;
using EtcdTerminal.Terminal;

namespace EtcdTerminal.Tests.Fakes;

public sealed class FakeTerminal : ITerminal
{
	public StringBuilder Output { get; } = new();

	public Queue<ConsoleKeyInfo> Keys { get; } = new();

	public int WindowWidth => 120;

	public int WindowHeight => 40;

	public int CursorLeft { get; set; }

	public int CursorTop { get; set; }

	public string Bg => "<bg>";

	public string DarkBg => "<darkbg>";

	public string White => "<white>";

	public string Grey => "<grey>";

	public string Green => "<green>";

	public string Red => "<red>";

	public string Teal => "<teal>";

	public string Yellow => "<yellow>";

	public string Dim => "<dim>";

	public string Accent => "<accent>";

	public string Reset => "</>";

	public bool KeyAvailable => Keys.Count > 0;

	public string SelectionPointer => "  ❯ ";

	public string Indent => "    ";

	public void Write(string text) => Output.Append(text);

	public void Write(string text, TerminalColor color) => Output.Append(text);

	public void WriteLine(string text) => Output.AppendLine(text);

	public void WriteLine(string text, TerminalColor color) => Output.AppendLine(text);

	public void WriteLine() => Output.AppendLine();

	public void WriteIndentedLine(string text) => Output.AppendLine(Indent + text);

	public void WriteIndentedLine(string text, TerminalColor color) => Output.AppendLine(Indent + text);

	public void Clear() => Output.Append("[clear]");

	public void SetCursorPosition(int left, int top)
	{
		CursorLeft = left;

		CursorTop = top;
	}

	public void SetBackground(string ansiColor)
	{
	}

	public void ResetBackground()
	{
	}

	public void ResetColor()
	{
	}

	public void SetDarkBackground()
	{
	}

	public string FillRow(string bg) => bg + new string(' ', WindowWidth) + Reset;

	public void WriteFillRow(string bg) => Output.AppendLine(FillRow(bg));

	public void WriteRow(string bg, string content) => Output.AppendLine(bg + content + Reset);

	public void WriteBorderedFillRow(string bg) => Output.AppendLine(bg + Reset);

	public void WriteBorderedRow(string bg, string content) => Output.AppendLine(bg + content + Reset);

	public void PadCurrentRow(string bg)
	{
	}

	public int GetVisibleLength(string s) => s.Length;

	public void Initialize()
	{
	}

	public ConsoleKeyInfo ReadKey()
	{
		if (Keys.Count == 0)
			throw new InvalidOperationException("No more keys");

		return Keys.Dequeue();
	}

	public void Flush()
	{
	}

	public void WriteException(Exception ex) => Output.Append("[exception:" + ex.Message + "]");

	public void SetCursorVisible(bool visible)
	{
	}

	public void WriteTable(TableData table) => Output.Append("[table]");

	public void WriteBanner(string text) => Output.Append("[banner]");

	public void ClearLine() => Output.Append("[clearline]");

	public void ClearToEndOfScreen() => Output.Append("[cleartoeos]");

	public void OnInterrupt(Action handler)
	{
	}

	public void Press(params ConsoleKey[] keys)
	{
		foreach (var key in keys)
			Keys.Enqueue(new ConsoleKeyInfo('\0', key, false, false, false));
	}
}
