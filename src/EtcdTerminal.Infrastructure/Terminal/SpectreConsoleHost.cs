using Spectre.Console;

namespace EtcdTerminal.Infrastructure.Terminal;

/// <summary>
/// Hands out the process wide console instance. The composition root must not
/// touch Spectre's static console directly, so the reference lives here, next
/// to the other production Spectre glue.
/// </summary>
public static class SpectreConsoleHost
{
	public static IAnsiConsole Default => AnsiConsole.Console;
}
