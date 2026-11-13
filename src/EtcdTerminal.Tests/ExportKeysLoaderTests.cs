using EtcdTerminal.App.Components;
using EtcdTerminal.App.Localization;
using EtcdTerminal.App.Screens.Keys.Export;
using EtcdTerminal.Configuration;
using EtcdTerminal.Environment;
using EtcdTerminal.Keys;
using EtcdTerminal.Security;
using EtcdTerminal.Session;
using EtcdTerminal.Tests.Fakes;
using NUnit.Framework;

namespace EtcdTerminal.Tests;

[TestFixture]
public sealed class ExportKeysLoaderTests
{
	[Test]
	public async Task LoadAsync_KeepsOnlyKeysUnderThePrefix()
	{
		var harness = new Harness(
		[
			new EtcdKeyValue { Key = "/app/db/host", Value = "localhost" },
			new EtcdKeyValue { Key = "/app/db/port", Value = "5432" },
			new EtcdKeyValue { Key = "/other/key", Value = "value" }
		]);

		var keys = await harness.Loader.LoadAsync("/app/");

		Assert.That(keys, Is.Not.Null);
		Assert.That(keys!.Select(kv => kv.Key), Is.EqualTo(new[] { "/app/db/host", "/app/db/port" }));
	}

	[Test]
	public async Task LoadAsync_EmptyPrefix_KeepsEveryReadableKey()
	{
		var harness = new Harness(
		[
			new EtcdKeyValue { Key = "/app/db/host", Value = "localhost" },
			new EtcdKeyValue { Key = "/other/key", Value = "value" }
		]);

		var keys = await harness.Loader.LoadAsync("");

		Assert.That(keys, Is.Not.Null);
		Assert.That(keys!.Select(kv => kv.Key), Is.EqualTo(new[] { "/app/db/host", "/other/key" }));
	}

	[Test]
	public async Task LoadAsync_NoMatchingKeys_ReturnsAnEmptyListInsteadOfNull()
	{
		var harness = new Harness([new EtcdKeyValue { Key = "/other/key", Value = "value" }]);

		var keys = await harness.Loader.LoadAsync("/app/");

		Assert.That(keys, Is.Not.Null);
		Assert.That(keys, Is.Empty);
	}

	private sealed class Harness
	{
		public readonly ExportKeysLoader Loader;

		public Harness(IReadOnlyList<EtcdKeyValue> readableKeys)
		{
			LocalizationCatalog localizations = new();
			FakeScreenCanvas canvas = new();
			FakeKeyReader keys = new();
			var session = ConnectedSession();
			var screen = new Screen(canvas, new Header(), new StatusBar(new StubAppInfo(), session, localizations));
			var spinner = new Spinner(keys, new FakeStatusIndicator());
			var load = new CancellableLoad(spinner, new Message(screen, keys, localizations), localizations);

			Loader = new ExportKeysLoader(new StubReadableKeys(readableKeys), session, load, localizations);
		}

		private static ConnectionSession ConnectedSession()
		{
			var session = new ConnectionSession();

			session.Start(new EtcdConnectionConfig { Name = "prod", ConnectionString = "http://localhost:2379" }, UserCapabilities.Unrestricted);

			return session;
		}
	}

	private sealed class StubReadableKeys(IReadOnlyList<EtcdKeyValue> _keys) : IReadableKeysProvider
	{
		public Task<IReadOnlyList<EtcdKeyValue>> GetReadableKeysAsync(UserCapabilities capabilities, CancellationToken ct = default) =>
			Task.FromResult(_keys);
	}

	private sealed class StubAppInfo : IAppInfo
	{
		public string Version => "0.0";
	}
}
