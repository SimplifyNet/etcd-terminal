using Spectre.Console;

namespace EtcdTerminal.Infrastructure.Terminal;

/// <summary>
/// The one console every Spectre consumer in this application shares. The
/// composition root injects it, so App wires a single instance instead of each
/// class reaching for the static <c>AnsiConsole.Console</c>, and App itself
/// never has to name a Spectre type to do it.
/// </summary>
public sealed class SpectreConsoleSource
{
	private readonly IAnsiConsole _console = AnsiConsole.Console;

	public IAnsiConsole Console => _console;
}
