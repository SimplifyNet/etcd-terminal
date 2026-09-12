namespace EtcdTerminal.Terminal;

public interface ITerminal
{
	int WindowWidth { get; }

	int WindowHeight { get; }

	int CursorLeft { get; }

	int CursorTop { get; }

	string Bg { get; }

	string DarkBg { get; }

	string White { get; }

	string Grey { get; }

	string Green { get; }

	string Red { get; }

	string Teal { get; }

	string Yellow { get; }

	string Dim { get; }

	string Accent { get; }

	string Reset { get; }

	string SelectionPointer { get; }

	string SelectionPointerEmpty { get; }

	string Indent => SelectionPointerEmpty;

	void Write(string text);

	void Write(string text, TerminalColor color);

	void WriteLine(string text);

	void WriteLine(string text, TerminalColor color);

	void WriteLine();

	void WriteIndentedLine(string text);

	void WriteIndentedLine(string text, TerminalColor color);

	void Clear();

	void SetCursorPosition(int left, int top);

	void SetBackground(string ansiColor);

	void ResetBackground();

	void ResetColor();

	void SetDarkBackground();

	void ClearScreen();

	string FillRow(string bg);

	void WriteFillRow(string bg);

	void WriteRow(string bg, string content);

	void WriteBorderedFillRow(string bg);

	void WriteBorderedRow(string bg, string content);

	void PadCurrentRow(string bg);

	int GetVisibleLength(string s);

	void Initialize();

	ConsoleKeyInfo ReadKey();

	void Flush();

	void WriteException(Exception ex);

	void SetCursorVisible(bool visible);

	Task ShowStatusAsync(string message, Func<CancellationToken, Task> action, TerminalColor color = TerminalColor.Warning);
}
