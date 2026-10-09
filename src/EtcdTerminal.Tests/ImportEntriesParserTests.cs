using EtcdTerminal.App.Screens.Keys.Import;
using NUnit.Framework;

namespace EtcdTerminal.Tests;

[TestFixture]
public sealed class ImportEntriesParserTests
{
	[Test]
	public void Parse_InvalidJson_ReportsTheInvalidJsonFailure()
	{
		var result = ImportEntriesParser.Parse(new ImportSource(":", "", "{bad"));

		Assert.Multiple(() =>
		{
			Assert.That(result.Entries, Is.Empty);
			Assert.That(result.Failure, Is.Not.Null);
			Assert.That(result.Failure!.Kind, Is.EqualTo(ImportParseFailureKind.InvalidJson));
			Assert.That(result.Failure.Detail, Is.Not.Null.And.Not.Empty);
		});
	}

	[Test]
	public void Parse_EmptyDocument_ReportsThatThereAreNoKeys()
	{
		var result = ImportEntriesParser.Parse(new ImportSource(":", "", "{}"));

		Assert.Multiple(() =>
		{
			Assert.That(result.Entries, Is.Empty);
			Assert.That(result.Failure, Is.Not.Null);
			Assert.That(result.Failure!.Kind, Is.EqualTo(ImportParseFailureKind.NoKeys));
		});
	}

	[Test]
	public void Parse_ArrayWithoutPrefix_ReportsThatThereAreNoKeys()
	{
		var result = ImportEntriesParser.Parse(new ImportSource(":", "", "[1,2]"));

		Assert.Multiple(() =>
		{
			Assert.That(result.Entries, Is.Empty);
			Assert.That(result.Failure, Is.Not.Null);
			Assert.That(result.Failure!.Kind, Is.EqualTo(ImportParseFailureKind.NoKeys));
		});
	}

	[Test]
	public void Parse_ValidDocument_ReturnsTheFlattenedEntries()
	{
		var result = ImportEntriesParser.Parse(new ImportSource(":", "", """{"a":{"b":"1"}}"""));

		Assert.Multiple(() =>
		{
			Assert.That(result.Failure, Is.Null);
			Assert.That(result.Entries, Is.EqualTo(new[] { new KeyValuePair<string, string>("a:b", "1") }));
		});
	}
}
