using EtcdTerminal.Presentation;
using EtcdTerminal.Terminal;

namespace EtcdTerminal.App.Components;

/// <summary>
/// Runs a long operation while an indicator is showing. The animation belongs
/// to Infrastructure; this component owns the policy that does not: Escape
/// cancels the operation, a completed operation reports success, and a failed
/// one reaches the caller unchanged. The console is handed back by the
/// indicator itself, so nothing here redraws a line or moves a cursor.
/// </summary>
public sealed class Spinner(ITerminal _terminal, IStatusIndicator _status)
{
	public async Task<bool> RunAsync(string message, Func<CancellationToken, Task> action)
	{
		using var cts = new CancellationTokenSource();

		var actionTask = action(cts.Token);

		try
		{
			await _status.RunAsync(new StyledText(message, TextRole.Accent), () => WaitWhileRunning(actionTask, cts));

			await actionTask;

			return true;
		}
		catch (OperationCanceledException)
		{
			return false;
		}
		finally
		{
			cts.Cancel();

			try
			{
				await actionTask;
			}
			catch
			{
			}
		}
	}

	private async Task WaitWhileRunning(Task actionTask, CancellationTokenSource cts)
	{
		while (!actionTask.IsCompleted)
		{
			if (PollEscape())
				cts.Cancel();

			await Task.WhenAny(actionTask, Task.Delay(50));
		}
	}

	private bool PollEscape()
	{
		try
		{
			return _terminal.KeyAvailable && _terminal.ReadKey().Key == ConsoleKey.Escape;
		}
		catch (InvalidOperationException)
		{
			return false;
		}
	}
}
