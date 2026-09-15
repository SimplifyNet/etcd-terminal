using EtcdTerminal.Terminal;

namespace EtcdTerminal.App.Components;

public sealed class Spinner(ITerminal _terminal)
{
	public async Task<bool> RunAsync(string message, Func<CancellationToken, Task> action)
	{
		const string frames = "⣷⣯⣟⡿⢿⣻⣽⣾";
		var frameIndex = 0;

		using var cts = new CancellationTokenSource();

		var spinnerTask = Task.Run(async () =>
		{
			while (!cts.Token.IsCancellationRequested)
			{
				if (PollEscape())
					cts.Cancel();

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
			_terminal.Write("\r\x1b[2K");
			_terminal.Flush();
		}, cts.Token);

		bool completed;

		try
		{
			await action(cts.Token);
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
