using EtcdTerminal.Infrastructure.Terminal;
using NUnit.Framework;

namespace EtcdTerminal.Tests;

[TestFixture]
public sealed class LineBufferTests
{
	[Test]
	public void StartsWithTheCaretAfterTheInitialText()
	{
		var buffer = new LineBuffer("abc");

		Assert.That(buffer.Cursor, Is.EqualTo(3));
	}

	[Test]
	public void InsertsAtTheCaret()
	{
		var buffer = new LineBuffer("ac");

		buffer.Apply(Key(ConsoleKey.LeftArrow));
		buffer.Apply(Char('b'));

		Assert.Multiple(() =>
		{
			Assert.That(buffer.Text, Is.EqualTo("abc"));
			Assert.That(buffer.Cursor, Is.EqualTo(2));
		});
	}

	[Test]
	public void HomeAndEndMoveToTheEnds()
	{
		var buffer = new LineBuffer("abc");

		buffer.Apply(Key(ConsoleKey.Home));
		Assert.That(buffer.Cursor, Is.EqualTo(0));

		buffer.Apply(Key(ConsoleKey.End));
		Assert.That(buffer.Cursor, Is.EqualTo(3));
	}

	[Test]
	public void BackspaceRemovesBeforeTheCaretAndDeleteAtIt()
	{
		var buffer = new LineBuffer("abcd");

		buffer.Apply(Key(ConsoleKey.LeftArrow));
		buffer.Apply(Key(ConsoleKey.LeftArrow));
		buffer.Apply(Key(ConsoleKey.Backspace));
		Assert.That(buffer.Text, Is.EqualTo("acd"));

		buffer.Apply(Key(ConsoleKey.Delete));
		Assert.That(buffer.Text, Is.EqualTo("ad"));
	}

	[Test]
	public void CaretDoesNotLeaveTheText()
	{
		var buffer = new LineBuffer("a");

		buffer.Apply(Key(ConsoleKey.RightArrow));
		Assert.That(buffer.Cursor, Is.EqualTo(1));

		buffer.Apply(Key(ConsoleKey.LeftArrow));
		buffer.Apply(Key(ConsoleKey.LeftArrow));
		Assert.That(buffer.Cursor, Is.EqualTo(0));

		buffer.Apply(Key(ConsoleKey.Backspace));
		Assert.That(buffer.Text, Is.EqualTo("a"));
	}

	[Test]
	public void ControlArrowsJumpWords()
	{
		var buffer = new LineBuffer("one two three");

		buffer.Apply(Key(ConsoleKey.LeftArrow, ConsoleModifiers.Control));
		Assert.That(buffer.Cursor, Is.EqualTo(8));

		buffer.Apply(Key(ConsoleKey.LeftArrow, ConsoleModifiers.Control));
		Assert.That(buffer.Cursor, Is.EqualTo(4));

		buffer.Apply(Key(ConsoleKey.RightArrow, ConsoleModifiers.Control));
		Assert.That(buffer.Cursor, Is.EqualTo(8));
	}

	[Test]
	public void ControlBackspaceRemovesTheWordBeforeTheCaret()
	{
		var buffer = new LineBuffer("one two");

		buffer.Apply(Key(ConsoleKey.Backspace, ConsoleModifiers.Control));

		Assert.That(buffer.Text, Is.EqualTo("one "));
	}

	[Test]
	public void KeepsLiteralBracketsAndUnicode()
	{
		var buffer = new LineBuffer();

		foreach (var c in "[ключ]")
			buffer.Apply(Char(c));

		Assert.That(buffer.Text, Is.EqualTo("[ключ]"));
	}

	[Test]
	public void IgnoresKeysItDoesNotEdit()
	{
		var buffer = new LineBuffer("a");

		Assert.Multiple(() =>
		{
			Assert.That(buffer.Apply(Key(ConsoleKey.Enter)), Is.False);
			Assert.That(buffer.Apply(Key(ConsoleKey.UpArrow)), Is.False);
			Assert.That(buffer.Apply(new ConsoleKeyInfo('\u0001', ConsoleKey.A, false, false, true)), Is.False);
			Assert.That(buffer.Text, Is.EqualTo("a"));
		});
	}

	private static ConsoleKeyInfo Key(ConsoleKey key, ConsoleModifiers modifiers = default) =>
		new('\0', key, modifiers.HasFlag(ConsoleModifiers.Shift), modifiers.HasFlag(ConsoleModifiers.Alt), modifiers.HasFlag(ConsoleModifiers.Control));

	private static ConsoleKeyInfo Char(char c) => new(c, ConsoleKey.NoName, false, false, false);
}
