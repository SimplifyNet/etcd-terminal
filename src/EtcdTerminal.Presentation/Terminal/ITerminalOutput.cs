namespace EtcdTerminal.Presentation.Terminal;

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

	void Flush();
}
