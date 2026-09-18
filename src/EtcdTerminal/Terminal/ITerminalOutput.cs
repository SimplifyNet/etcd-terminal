namespace EtcdTerminal.Terminal;

public interface ITerminalOutput
{
	int WindowWidth { get; }

	int WindowHeight { get; }

	void Write(string text);

	void Write(string text, TerminalColor color);

	void WriteLine(string text);

	void WriteLine(string text, TerminalColor color);

	void WriteLine();

	void WriteIndentedLine(string text);

	void WriteIndentedLine(string text, TerminalColor color);

	void Clear();

	void ClearLine();

	void ClearToEndOfScreen();

	string FillRow(string bg);

	void WriteFillRow(string bg);

	void WriteRow(string bg, string content);

	void WriteBorderedFillRow(string bg);

	void WriteBorderedRow(string bg, string content);

	void PadCurrentRow(string bg);

	int GetVisibleLength(string s);

	void Flush();
}
