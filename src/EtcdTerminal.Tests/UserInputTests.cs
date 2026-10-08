using EtcdTerminal.App.Components;
using EtcdTerminal.App.Engine;
using EtcdTerminal.Configuration;
using EtcdTerminal.Presentation;
using NUnit.Framework;

namespace EtcdTerminal.Tests;

[TestFixture]
public sealed class UserInputTests
{
	[Test]
	public void Ask_TrimsWhenTheSettingIsOn()
	{
		var harness = new Harness(trim: true);

		harness.TextInput.Answers.Enqueue("  hi  ");

		Assert.That(harness.Input.Ask("Enter:"), Is.EqualTo("hi"));
	}

	[Test]
	public void Ask_KeepsWhitespaceWhenTheSettingIsOff()
	{
		var harness = new Harness(trim: false);

		harness.TextInput.Answers.Enqueue("  hi  ");

		Assert.That(harness.Input.Ask("Enter:"), Is.EqualTo("  hi  "));
	}

	[Test]
	public void Secret_FollowsTheTrimSetting()
	{
		var harness = new Harness(trim: false);

		harness.TextInput.Answers.Enqueue("  pw  ");

		Assert.That(harness.Input.Secret("Enter:"), Is.EqualTo("  pw  "));
	}

	private sealed class Harness
	{
		public readonly QueueTextInput TextInput = new();
		public readonly UserInput Input;

		public Harness(bool trim)
		{
			var settings = new AppSettingsStore(new StubSettingsRepository(trim));

			settings.Reload();
			Input = new(new Prompt(TextInput), settings);
		}
	}

	private sealed class StubSettingsRepository(bool _trim) : IAppSettingsRepository
	{
		public AppSettings Load() => new() { TrimInputValues = _trim };

		public void Save(AppSettings settings)
		{
		}
	}

	private sealed class QueueTextInput : ITextInput
	{
		public readonly Queue<string?> Answers = new();

		public string? ReadLine(string prompt, string? defaultValue = null) => Answers.Dequeue();

		public string? ReadSecret(string prompt) => Answers.Dequeue();
	}
}
