using System.Text.Json;
using EtcdTerminal.Keys;
using NUnit.Framework;

namespace EtcdTerminal.Tests;

[TestFixture]
public sealed class JsonKeyExporterTests
{
	[Test]
	public void Export_WritesEveryKeyAsAPropertyOfOneObject()
	{
		var json = JsonKeyExporter.Export(
		[
			new EtcdKeyValue { Key = "/app/db/host", Value = "localhost" },
			new EtcdKeyValue { Key = "/app/db/port", Value = "5432" }
		]);

		using var document = JsonDocument.Parse(json);
		var root = document.RootElement;

		Assert.Multiple(() =>
		{
			Assert.That(root.ValueKind, Is.EqualTo(JsonValueKind.Object));
			Assert.That(root.GetProperty("/app/db/host").GetString(), Is.EqualTo("localhost"));
			Assert.That(root.GetProperty("/app/db/port").GetString(), Is.EqualTo("5432"));
		});
	}

	[Test]
	public void Export_KeepsLiteralCharactersAndEmptyValues()
	{
		var json = JsonKeyExporter.Export(
		[
			new EtcdKeyValue { Key = "/app/[quoted] \\ key", Value = "line 1\nline 2 — значение" },
			new EtcdKeyValue { Key = "/app/empty", Value = "" }
		]);

		using var document = JsonDocument.Parse(json);
		var root = document.RootElement;

		Assert.Multiple(() =>
		{
			Assert.That(root.GetProperty("/app/[quoted] \\ key").GetString(), Is.EqualTo("line 1\nline 2 — значение"));
			Assert.That(root.GetProperty("/app/empty").GetString(), Is.Empty);
		});
	}

	[Test]
	public void Export_NoKeys_WritesAnEmptyObjectThatImportsAsNoKeys()
	{
		var json = JsonKeyExporter.Export([]);

		using var document = JsonDocument.Parse(json);

		Assert.That(document.RootElement.ValueKind, Is.EqualTo(JsonValueKind.Object));
		Assert.That(document.RootElement.EnumerateObject(), Is.Empty);
	}
}
