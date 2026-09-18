namespace EtcdTerminal.Terminal;

public interface ITerminalStyle
{
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

	string Indent { get; }

	void SetBackground(string ansiColor);

	void ResetBackground();

	void ResetColor();

	void SetDarkBackground();
}
