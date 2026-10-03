using EtcdTerminal.Presentation;
using Spectre.Console;

namespace EtcdTerminal.Infrastructure.Terminal;

/// <summary>
/// Reads console input through Spectre, which is already the input the prompts
/// and the selection prompts read from, so one key queue is shared by all of
/// them.
/// </summary>
public sealed class SpectreKeyReader(IAnsiConsole _console) : IKeyReader
{
	public bool KeyAvailable => _console.Input.IsKeyAvailable();

	public ConsoleKeyInfo ReadKey() =>
		_console.Input.ReadKey(true) ?? throw new InvalidOperationException("No key available.");
}
