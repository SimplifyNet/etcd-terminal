using EtcdTerminal.Terminal;

namespace EtcdTerminal.App.Components;

public sealed class Spinner(ITerminal _terminal)
{
	public async Task RunAsync(string message, Func<CancellationToken, Task> action)
	{
		const string frames = "⣾⣽⣻⢿⡿⣟⣯⣷";
		var frameIndex = 0;
		var done = false;

		var spinnerTask = Task.Run(async () =>
		{
			while (!done)
			{
				_terminal.SetCursorVisible(false);
				_terminal.Write("\r" + _terminal.Indent + _terminal.Accent + frames[frameIndex] + _terminal.Reset + " " + message);
				_terminal.Flush();
				frameIndex = (frameIndex + 1) % frames.Length;
				await Task.Delay(100);
			}
			_terminal.Write("\r" + new string(' ', _terminal.Indent.Length + message.Length + 2) + "\r");
			_terminal.Flush();
		});

		try
		{
			await action(CancellationToken.None);
		}
		finally
		{
			done = true;
			await spinnerTask;
			_terminal.SetCursorVisible(false);
		}
	}
}