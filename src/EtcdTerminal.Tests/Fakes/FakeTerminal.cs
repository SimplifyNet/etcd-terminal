using System.Text;
using EtcdTerminal.Presentation.Terminal;

namespace EtcdTerminal.Tests.Fakes;

public sealed class FakeTerminal : ITerminal
{
	public StringBuilder Output { get; } = new();

	public Queue<ConsoleKeyInfo> Keys { get; } = new();

	public int WindowWidth => 120;

	public int WindowHeight => 40;

	public int CursorLeft { get; set; }

	public int CursorTop { get; set; }

	public string Subtle => "<subtle>";

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

	public void ResetBackground()
	{
	}

	public void Initialize()
	{
	}

	public void Shutdown()
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

	public void OnInterrupt(Action handler)
	{
	}

	public void Press(params ConsoleKey[] keys)
	{
		foreach (var key in keys)
			Keys.Enqueue(new ConsoleKeyInfo('\0', key, false, false, false));
	}
}
