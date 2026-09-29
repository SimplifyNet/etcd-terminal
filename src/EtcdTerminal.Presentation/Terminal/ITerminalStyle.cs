namespace EtcdTerminal.Terminal;

public interface ITerminalStyle
{
	string Subtle { get; }

	string Accent { get; }

	string Reset { get; }

	string SelectionPointer { get; }

	string Indent { get; }

	void ResetBackground();
}
