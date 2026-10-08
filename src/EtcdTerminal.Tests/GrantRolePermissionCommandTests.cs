using EtcdTerminal.App.Components;
using EtcdTerminal.App.Engine;
using EtcdTerminal.App.Localization;
using EtcdTerminal.App.Screens.Roles;
using EtcdTerminal.App.Screens.Roles.Commands;
using EtcdTerminal.Configuration;
using EtcdTerminal.Environment;
using EtcdTerminal.Permissions;
using EtcdTerminal.Presentation;
using EtcdTerminal.Session;
using EtcdTerminal.Tests.Fakes;
using NUnit.Framework;

namespace EtcdTerminal.Tests;

[TestFixture]
public sealed class GrantRolePermissionCommandTests
{
	[Test]
	public async Task ExecuteAsync_AsksWithThePromptsInTheirOriginalOrder()
	{
		var harness = new Harness();

		harness.TextInput.Answers.Enqueue("admin");
		harness.Answers.Answer(PermissionScope.Key);
		harness.TextInput.Answers.Enqueue("/app/a");
		harness.Answers.Answer(PermissionType.Read);
		harness.Keys.Press(ConsoleKey.Enter);

		await harness.Command.ExecuteAsync();

		Assert.Multiple(() =>
		{
			Assert.That(harness.TextInput.Prompts, Is.EqualTo(new[] { "Enter role name:", "Enter exact key:" }));
			Assert.That(harness.Answers.Prompt<PermissionScope>(0).Title, Is.EqualTo("Grant access to a single key or to a prefix?"));
			Assert.That(harness.Answers.Prompt<PermissionType>(1).Title, Is.EqualTo("Select permission type:"));
			Assert.That(harness.Admin.Granted, Is.EqualTo(new[] { ("admin", PermissionType.Read, "/app/a", PermissionScope.Key) }));
		});
	}

	[Test]
	public async Task ExecuteAsync_PrefixScope_AsksWithThePrefixKeyPrompt()
	{
		var harness = new Harness();

		harness.TextInput.Answers.Enqueue("admin");
		harness.Answers.Answer(PermissionScope.Prefix);
		harness.TextInput.Answers.Enqueue("/app/");
		harness.Answers.Answer(PermissionType.Write);
		harness.Keys.Press(ConsoleKey.Enter);

		await harness.Command.ExecuteAsync();

		Assert.Multiple(() =>
		{
			Assert.That(harness.TextInput.Prompts, Is.EqualTo(new[] { "Enter role name:", "Enter key prefix:" }));
			Assert.That(harness.Admin.Granted, Is.EqualTo(new[] { ("admin", PermissionType.Write, "/app/", PermissionScope.Prefix) }));
		});
	}

	[Test]
	public async Task ExecuteAsync_RolePromptCancelled_CallsNoAdminMethod()
	{
		var harness = new Harness();

		harness.TextInput.Answers.Enqueue(null);

		await harness.Command.ExecuteAsync();

		Assert.That(harness.Admin.Calls, Is.Zero, "a cancelled prompt never reaches the administration API");
	}

	[Test]
	public async Task ExecuteAsync_ScopePromptCancelled_CallsNoAdminMethod()
	{
		var harness = new Harness();

		harness.TextInput.Answers.Enqueue("admin");
		harness.Answers.Cancel();

		await harness.Command.ExecuteAsync();

		Assert.That(harness.Admin.Calls, Is.Zero, "a cancelled prompt never reaches the administration API");
	}

	[Test]
	public async Task ExecuteAsync_KeyPromptCancelled_CallsNoAdminMethod()
	{
		var harness = new Harness();

		harness.TextInput.Answers.Enqueue("admin");
		harness.Answers.Answer(PermissionScope.Key);
		harness.TextInput.Answers.Enqueue(null);

		await harness.Command.ExecuteAsync();

		Assert.That(harness.Admin.Calls, Is.Zero, "a cancelled prompt never reaches the administration API");
	}

	[Test]
	public async Task ExecuteAsync_TypePromptCancelled_CallsNoAdminMethod()
	{
		var harness = new Harness();

		harness.TextInput.Answers.Enqueue("admin");
		harness.Answers.Answer(PermissionScope.Key);
		harness.TextInput.Answers.Enqueue("/app/a");
		harness.Answers.Cancel();

		await harness.Command.ExecuteAsync();

		Assert.That(harness.Admin.Calls, Is.Zero, "a cancelled prompt never reaches the administration API");
	}

	private sealed class Harness
	{
		public readonly QueueTextInput TextInput = new();
		public readonly FakeSelectionPrompt Answers = new();
		public readonly FakeKeyReader Keys = new();
		public readonly FakeRoleAdmin Admin = new();
		public readonly GrantRolePermissionCommand Command;

		public Harness()
		{
			var localization = new EnglishLocalization();
			var statusBar = new StatusBar(new StubAppInfo(), new ConnectionSession(), localization);
			var screen = new Screen(new FakeScreenCanvas(), new Header(), statusBar);
			var menu = new Menu(screen, Answers);
			var settings = new AppSettingsStore(new FakeSettingsRepository());
			var input = new UserInput(new Prompt(TextInput), settings);
			var scope = new PermissionScopeSelector(menu, localization);
			var type = new PermissionTypeSelector(menu, localization);
			var targets = new PermissionTargetPrompt(input, scope, type, localization);

			Command = new GrantRolePermissionCommand(Admin, targets, new Message(screen, Keys, localization), localization);
		}
	}

	private sealed class QueueTextInput : ITextInput
	{
		public readonly Queue<string?> Answers = new();

		public List<string> Prompts { get; } = [];

		public string? ReadLine(string prompt, string? defaultValue = null)
		{
			Prompts.Add(prompt);

			return Answers.Dequeue();
		}

		public string? ReadSecret(string prompt)
		{
			Prompts.Add(prompt);

			return Answers.Dequeue();
		}
	}

	private sealed class StubAppInfo : IAppInfo
	{
		public string Version => "0.0";
	}
}
