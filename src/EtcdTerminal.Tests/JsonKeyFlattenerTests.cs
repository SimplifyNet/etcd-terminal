using System.Text.Json;
using EtcdTerminal.Keys;
using NUnit.Framework;

namespace EtcdTerminal.Tests;

[TestFixture]
public sealed class JsonKeyFlattenerTests
{
	[Test]
	public void Flatten_NestedObject_BuildsColonPath()
	{
		var entries = JsonKeyFlattener.Flatten("{\"a\":{\"b\":{\"c\":1}}}", "", ":");

		Assert.That(entries.Select(kv => kv.Key + "=" + kv.Value), Is.EquivalentTo(["a:b:c=1"]));
	}

	[Test]
	public void Flatten_Array_UsesIndexes()
	{
		var entries = JsonKeyFlattener.Flatten("{\"arr\":[1,2]}", "", ":");

		Assert.That(entries.Select(kv => kv.Key + "=" + kv.Value), Is.EquivalentTo(["arr:0=1", "arr:1=2"]));
	}

	[Test]
	public void Flatten_TrailingComma_Tolerated()
	{
		var entries = JsonKeyFlattener.Flatten("{\"a\":1,}", "", ":");

		Assert.That(entries.Select(kv => kv.Key + "=" + kv.Value), Is.EquivalentTo(["a=1"]));
	}

	[Test]
	public void Flatten_BarePair_WrappedInBraces()
	{
		var entries = JsonKeyFlattener.Flatten("\"a\": 1", "", ":");

		Assert.That(entries.Select(kv => kv.Key + "=" + kv.Value), Is.EquivalentTo(["a=1"]));
	}

	[Test]
	public void Flatten_ArrayRoot_WithoutPrefix_Throws()
	{
		Assert.Throws<ArgumentException>(() => JsonKeyFlattener.Flatten("[1,2]", "", ":"));
	}

	[Test]
	public void Flatten_InvalidJson_ThrowsJsonException()
	{
		Assert.That(() => JsonKeyFlattener.Flatten("{bad", "", ":"), Throws.InstanceOf<JsonException>());
	}
}
