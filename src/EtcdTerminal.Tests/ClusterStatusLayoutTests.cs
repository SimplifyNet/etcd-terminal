using EtcdTerminal.App.Localization;
using EtcdTerminal.App.Screens.Cluster;
using EtcdTerminal.Cluster;
using EtcdTerminal.Presentation;
using NUnit.Framework;

namespace EtcdTerminal.Tests;

[TestFixture]
public sealed class ClusterStatusLayoutTests
{
	[Test]
	public void Rows_MapHealthAndStatusMetrics()
	{
		var layout = new ClusterStatusLayout(new LocalizationCatalog());
		var members = new[]
		{
			new EtcdClusterMember
			{
				Id = 0x1234,
				Name = "node/[a]",
				Version = "3.7.1",
				IsLeader = true,
				IsHealthy = true,
				HasStatus = true,
				DbSize = 2000,
				DbSizeInUse = 1000,
				RaftTerm = 5,
				RaftIndex = 100,
				RaftAppliedIndex = 99,
				Errors = []
			},
			new EtcdClusterMember
			{
				Id = 0xabcd,
				Name = string.Empty,
				Version = string.Empty,
				IsLeader = false,
				IsHealthy = false,
				HasStatus = false
			}
		};

		var rows = layout.Rows(members);

		Assert.Multiple(() =>
		{
			Assert.That(layout.Headers().Select(span => span.Text), Is.EqualTo(new[]
			{
				"Name",
				"Healthy",
				"Member ID",
				"Version",
				"Is Leader",
				"DB Size",
				"In Use",
				"% Not In Use",
				"Raft Term",
				"Raft Index",
				"Raft Applied Index",
				"Errors"
			}));
			Assert.That(layout.Headers().Select(span => span.Role), Is.All.EqualTo(TextRole.Accent));
			Assert.That(Texts(rows[0]), Is.EqualTo(new[]
			{
				"node/[a]", "Yes", "1234", "3.7.1", "Yes", "2 KB", "1 KB", "50.0%", "5", "100", "99", "none"
			}));
			Assert.That(Texts(rows[1]), Is.EqualTo(new[]
			{
				"none", "No", "abcd", "none", "No", "none", "none", "none", "none", "none", "none", "none"
			}));
			Assert.That(layout.Rows([]), Is.Empty);
		});
	}

	[Test]
	public void Rows_ReportErrorsAndNormalizeControlCharacters()
	{
		var layout = new ClusterStatusLayout(new LocalizationCatalog());
		var members = new[]
		{
			new EtcdClusterMember
			{
				Name = "node\nname",
				Version = "3.7\t1",
				HasStatus = true,
				DbSize = 0,
				Errors = ["disk\nfull", "slow\tapply"]
			}
		};

		var row = layout.Rows(members).Single();

		Assert.Multiple(() =>
		{
			Assert.That(row[0].Text, Is.EqualTo("node\u23CEname"));
			Assert.That(row[3].Text, Is.EqualTo("3.7\u21921"));
			Assert.That(row[7].Text, Is.EqualTo("none"), "a zero database size has no usage percentage");
			Assert.That(row[11].Text, Is.EqualTo("disk\u23CEfull; slow\u2192apply"));
		});
	}

	[Test]
	public void PercentageNotInUse_KeepsOneDecimalPlace()
	{
		var layout = new ClusterStatusLayout(new LocalizationCatalog());
		var members = new[]
		{
			new EtcdClusterMember
			{
				Name = "node",
				HasStatus = true,
				DbSize = 827,
				DbSizeInUse = 823
			}
		};

		var row = layout.Rows(members).Single();

		Assert.That(row[7].Text, Is.EqualTo("0.5%"));
	}

	private static IReadOnlyList<string> Texts(IReadOnlyList<StyledText> spans) =>
		[.. spans.Select(span => span.Text)];
}
