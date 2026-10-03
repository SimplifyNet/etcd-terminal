using EtcdTerminal.Presentation;

namespace EtcdTerminal.Tests.Fakes;

/// <summary>
/// Serves queued keys instead of reading the console, so a component that waits
/// for a key returns as soon as the test has queued it.
/// </summary>
public sealed class FakeKeyReader : IKeyReader
{
	public FakeKeyReader() => Keys = new();

	/// The real components all read the same console input, so a test that uses
	/// both the terminal and a key reader shares one queue.
	public FakeKeyReader(Queue<ConsoleKeyInfo> keys) => Keys = keys;

	public Queue<ConsoleKeyInfo> Keys { get; }

	public bool KeyAvailable => Keys.Count > 0;

	public ConsoleKeyInfo ReadKey()
	{
		if (Keys.Count == 0)
			throw new InvalidOperationException("No more keys");

		return Keys.Dequeue();
	}

	public void Press(ConsoleKey key) => Keys.Enqueue(new ConsoleKeyInfo('\0', key, false, false, false));

	public void Press(char key) => Keys.Enqueue(new ConsoleKeyInfo(key, ConsoleKey.None, false, false, false));
}
