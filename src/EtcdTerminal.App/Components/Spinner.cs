using EtcdTerminal.Terminal;

namespace EtcdTerminal.App.Components;

public sealed class Spinner(ITerminal _terminal)
{
	public async Task<bool> RunAsync(string message, Func<CancellationToken, Task> action)
	{
		const string frames = "⣷⣯⣟⡿⢿⣻⣽⣾";
		var frameIndex = 0;

		using var cts = new CancellationTokenSource();

		var actionTask = action(cts.Token);

		var spinnerTask = Task.Run(async () =>
		{
			while (!actionTask.IsCompleted)
			{
				_terminal.SetCursorVisible(false);
				_terminal.Write("\r" + _terminal.Indent + _terminal.Accent + frames[frameIndex] + _terminal.Reset + " " + message);
				_terminal.Flush();
				frameIndex = (frameIndex + 1) % frames.Length;

				try
				{
					await Task.Delay(100, cts.Token);
				}
				catch (OperationCanceledException)
				{
					break;
				}
			}
			_terminal.ClearLine();
			_terminal.Flush();
		}, cts.Token);

		bool completed;

		try
		{
			while (!actionTask.IsCompleted)
			{
				if (PollEscape())
					cts.Cancel();

				await Task.WhenAny(actionTask, Task.Delay(50));
			}

			await actionTask;
			completed = true;
		}
		catch (OperationCanceledException)
		{
			completed = false;
		}
		finally
		{
			cts.Cancel();

			try
			{
				await spinnerTask;
			}
			catch
			{
			}

			_terminal.SetCursorVisible(false);
		}

		return completed;
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
