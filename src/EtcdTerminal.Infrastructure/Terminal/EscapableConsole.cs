using Spectre.Console;
using Spectre.Console.Rendering;

namespace EtcdTerminal.Infrastructure.Terminal;

public sealed class EscapableConsole(IAnsiConsole inner) : IAnsiConsole
{
	private readonly IAnsiConsoleInput _input = new EscapableInput(inner.Input);

	public Profile Profile => inner.Profile;

	public IAnsiConsoleCursor Cursor => inner.Cursor;

	public IAnsiConsoleInput Input => _input;

	public IExclusivityMode ExclusivityMode => inner.ExclusivityMode;

	public RenderPipeline Pipeline => inner.Pipeline;

	public void Clear(bool home) => inner.Clear(home);

	public void Write(IRenderable renderable) => inner.Write(renderable);

	public void WriteAnsi(Action<AnsiWriter> action) => inner.WriteAnsi(action);

	private sealed class EscapableInput(IAnsiConsoleInput inner) : IAnsiConsoleInput
	{
		public bool IsKeyAvailable() => inner.IsKeyAvailable();

		public ConsoleKeyInfo? ReadKey(bool intercept)
		{
			var key = inner.ReadKey(intercept);

			if (key is { Key: ConsoleKey.Escape })
				throw new OperationCanceledException("Prompt cancelled by user.");

			return key;
		}

		public async Task<ConsoleKeyInfo?> ReadKeyAsync(bool intercept, CancellationToken cancellationToken)
		{
			var key = await inner.ReadKeyAsync(intercept, cancellationToken).ConfigureAwait(false);

			if (key is { Key: ConsoleKey.Escape })
				throw new OperationCanceledException("Prompt cancelled by user.");

			return key;
		}
	}
}
