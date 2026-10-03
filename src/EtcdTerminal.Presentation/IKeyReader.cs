namespace EtcdTerminal.Presentation;

/// <summary>
/// Reads keys from the console. Components ask for the next key instead of
/// touching console input themselves.
/// </summary>
public interface IKeyReader
{
	bool KeyAvailable { get; }

	ConsoleKeyInfo ReadKey();
}
