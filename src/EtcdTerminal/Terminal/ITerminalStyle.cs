namespace EtcdTerminal.Terminal;

public interface ITerminalStyle
{
	string PanelBackground { get; }

	string PanelDarkerBackground { get; }

	string Primary { get; }

	string Secondary { get; }

	string Success { get; }

	string Danger { get; }

	string Warning { get; }

	string Muted { get; }

	string Subtle { get; }

	string Accent { get; }

	string Reset { get; }

	string SelectionPointer { get; }

	string Indent { get; }

	void SetBackground(string ansiColor);

	void ResetBackground();

	void ResetColor();

	void SetDarkBackground();
}
