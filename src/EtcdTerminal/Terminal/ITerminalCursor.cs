namespace EtcdTerminal.Terminal;

public interface ITerminalCursor
{
	int CursorLeft { get; }

	int CursorTop { get; }

	void SetCursorPosition(int left, int top);

	void SetCursorVisible(bool visible);
}
