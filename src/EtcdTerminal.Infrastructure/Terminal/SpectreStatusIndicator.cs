using EtcdTerminal.Presentation;
using Spectre.Console;

namespace EtcdTerminal.Infrastructure.Terminal;

/// <summary>
/// Runs an operation behind Spectre's Status, which already owns the animated
/// frames, the cursor and the region it used: it clears that region when the
/// action ends, which is what the previous line redraw and ClearLine did by
/// hand. The message is escaped because a status is markup to Spectre and must
/// reach the screen as the literal text the component supplied.
/// </summary>
public sealed class SpectreStatusIndicator(IAnsiConsole _console, RoleStyleMapper _styles) : IStatusIndicator
{
	public Task RunAsync(StyledText message, Func<Task> action) =>
		new Status(_console)
		{
			Spinner = Spinner.Known.Dots,
			SpinnerStyle = _styles.Resolve(message.Role)
		}
		.StartAsync(Markup.Escape(message.Text), _ => action());
}
