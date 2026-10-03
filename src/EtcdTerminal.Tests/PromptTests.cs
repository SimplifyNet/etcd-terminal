using EtcdTerminal.App.Engine;
using EtcdTerminal.Presentation.Terminal;
using NUnit.Framework;

namespace EtcdTerminal.Tests;

[TestFixture]
public sealed class PromptTests
{
	[Test]
	public void Ask_TrimsWhenRequested()
	{
		var harness = new Harness();

		harness.TextInput.Answers.Enqueue("  hi  ");

		Assert.That(harness.Prompt.Ask("Enter:", trim: true), Is.EqualTo("hi"));
	}

	[Test]
	public void Ask_PreservesWhitespaceWhenTrimDisabled()
	{
		var harness = new Harness();

		harness.TextInput.Answers.Enqueue("  hi  ");

		Assert.That(harness.Prompt.Ask("Enter:", trim: false), Is.EqualTo("  hi  "));
	}

	[Test]
	public void Ask_WhitespaceOnlyWithTrimDisabledAndAllowEmpty_PreservedByteForByte()
	{
		var harness = new Harness();

		harness.TextInput.Answers.Enqueue("   ");

		Assert.That(harness.Prompt.Ask("Enter:", allowEmpty: true, trim: false), Is.EqualTo("   "));
	}

	[Test]
	public void Ask_WhitespaceOnlyWithTrim_RequiredReturnsNull()
	{
		var harness = new Harness();

		harness.TextInput.Answers.Enqueue("   ");

		Assert.That(harness.Prompt.Ask("Enter:", trim: true), Is.Null);
	}

	[Test]
	public void Ask_EmptyWithAllowEmpty_ReturnsEmptyNotCancellation()
	{
		var harness = new Harness();

		harness.TextInput.Answers.Enqueue(string.Empty);

		Assert.That(harness.Prompt.Ask("Enter:", allowEmpty: true), Is.EqualTo(string.Empty));
	}

	[Test]
	public void Ask_EmptyRequired_ReturnsNull()
	{
		var harness = new Harness();

		harness.TextInput.Answers.Enqueue(string.Empty);

		Assert.That(harness.Prompt.Ask("Enter:"), Is.Null);
	}

	[Test]
	public void Ask_Cancelled_ReturnsNull()
	{
		var harness = new Harness();

		harness.TextInput.Answers.Enqueue(null);
		harness.TextInput.Answers.Enqueue(null);

		Assert.That(harness.Prompt.Ask("Enter:"), Is.Null);
		Assert.That(harness.Prompt.Ask("Enter:", "default"), Is.Null);
	}

	[Test]
	public void Ask_DefaultValue_PreservesWithoutTrim()
	{
		var harness = new Harness();

		harness.TextInput.Answers.Enqueue("  x  ");

		Assert.That(harness.Prompt.Ask("Enter:", "default", trim: false), Is.EqualTo("  x  "));
	}

	[Test]
	public void Secret_TrimsWhenRequested()
	{
		var harness = new Harness();

		harness.TextInput.Answers.Enqueue("  pw  ");

		Assert.That(harness.Prompt.Secret("Enter:", trim: true), Is.EqualTo("pw"));
	}

	[Test]
	public void Secret_PreservesWithoutTrim()
	{
		var harness = new Harness();

		harness.TextInput.Answers.Enqueue("  pw  ");

		Assert.That(harness.Prompt.Secret("Enter:", trim: false), Is.EqualTo("  pw  "));
	}

	private sealed class Harness
	{
		public readonly QueueTextInput TextInput = new();
		public readonly Prompt Prompt;

		public Harness()
		{
			Prompt = new(TextInput);
		}
	}

	private sealed class QueueTextInput : ITextInput
	{
		public readonly Queue<string?> Answers = new();

		public string? ReadLine(string prompt, string? defaultValue = null) => Answers.Dequeue();

		public string? ReadSecret(string prompt) => Answers.Dequeue();
	}
}
