using Spectre.Console;

namespace EtcdTerminal.Infrastructure.Terminal;

/// <summary>
/// Hands out the process wide console instance. The composition root must not
/// touch Spectre's static console directly, so the reference lives here, next
/// to the other production Spectre glue.
/// </summary>
public static class SpectreConsoleHost
{
	/// The console is captured on first access, so the encoding must be right
	/// before that happens; <see cref="AnsiConsole.Console"/> never changes it.
	static SpectreConsoleHost() => ConsoleTerminalSession.ConfigureOutputEncoding();

	public static IAnsiConsole Default => AnsiConsole.Console;
}
