namespace EtcdTerminal.Terminal;

public interface ITerminalInput
{
	bool KeyAvailable { get; }

	ConsoleKeyInfo ReadKey();
}
