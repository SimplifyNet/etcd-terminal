namespace EtcdTerminal.Presentation.Terminal;

public interface ITerminalInput
{
	bool KeyAvailable { get; }

	ConsoleKeyInfo ReadKey();
}
