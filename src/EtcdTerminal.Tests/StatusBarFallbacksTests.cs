using EtcdTerminal.Presentation;
using NUnit.Framework;

namespace EtcdTerminal.Tests;

[TestFixture]
public sealed class StatusBarFallbacksTests
{
	private const int EndpointTruncationWidth = 20;

	[Test]
	public void InPriorityOrder_StartsUnchanged_AndEndsWithTheVersionOnly()
	{
		var full = Full();

		var candidates = StatusBarFallbacks.InPriorityOrder(full).ToList();

		Assert.Multiple(() =>
		{
			Assert.That(candidates, Is.Not.Empty);
			Assert.That(candidates[0], Is.SameAs(full));
			Assert.That(candidates[^1].Name, Is.Null);
			Assert.That(candidates[^1].Connection, Is.Null);
			Assert.That(candidates[^1].Username, Is.Null);
			Assert.That(candidates[^1].Hints, Is.Empty);
			Assert.That(candidates[^1].Version, Is.EqualTo(full.Version));
		});
	}

	[Test]
	public void InPriorityOrder_DropsTheUsernameBeforeTheEndpointAndTheNameLast()
	{
		var full = Full();

		var candidates = StatusBarFallbacks.InPriorityOrder(full).ToList();

		var username = candidates.FindIndex(candidate => candidate.Username is null && candidate.Connection is not null);
		var endpoint = candidates.FindIndex(candidate => candidate.Username is null && candidate.Connection is null && candidate.Hints.Count > 0);
		var hints = candidates.FindIndex(candidate => candidate.Hints.Count == 0 && candidate.Name is not null);
		var name = candidates.FindIndex(candidate => candidate.Name is null);

		Assert.Multiple(() =>
		{
			Assert.That(username, Is.GreaterThan(0));
			Assert.That(endpoint, Is.GreaterThan(username));
			Assert.That(hints, Is.GreaterThan(endpoint));
			Assert.That(name, Is.EqualTo(candidates.Count - 1));
			Assert.That(name, Is.GreaterThan(hints));
		});
	}

	[Test]
	public void InPriorityOrder_ShortenedEndpoints_NeverSplitASurrogatePair()
	{
		var full = Full() with { Connection = new StyledText("https://example.com😀/path", TextRole.Muted) };

		var shortened = ShortenedEndpoints(full);

		Assert.Multiple(() =>
		{
			Assert.That(shortened, Is.Not.Empty);
			Assert.That(shortened.Where(HasLoneSurrogate), Is.Empty);
		});
	}

	[Test]
	public void InPriorityOrder_ShortenedEndpoints_NeverFallBelowTheTruncationWidth()
	{
		var full = Full();

		var shortened = ShortenedEndpoints(full);

		Assert.Multiple(() =>
		{
			Assert.That(shortened, Is.Not.Empty);
			Assert.That(shortened.Where(text => text.Length < EndpointTruncationWidth), Is.Empty);
		});
	}

	private static StatusBarModel Full() =>
		new()
		{
			Hints = [new StyledText("↑/↓", TextRole.Primary), new StyledText(" Enter", TextRole.Muted)],
			Name = new StyledText("prod", TextRole.Secondary),
			Connection = new StyledText("https://api.example.com:2379", TextRole.Muted),
			Username = new StyledText("root", TextRole.Warning),
			Version = new StyledText("1.0.0", TextRole.Primary)
		};

	private static List<string> ShortenedEndpoints(StatusBarModel full) =>
		StatusBarFallbacks.InPriorityOrder(full)
			.Where(candidate => candidate.Connection is not null && candidate.Connection.Text != full.Connection!.Text)
			.Select(candidate => candidate.Connection!.Text)
			.ToList();

	private static bool HasLoneSurrogate(string text)
	{
		for (var i = 0; i < text.Length; i++)
		{
			if (char.IsHighSurrogate(text[i]) && (i + 1 >= text.Length || !char.IsLowSurrogate(text[i + 1])))
				return true;

			if (char.IsLowSurrogate(text[i]) && (i == 0 || !char.IsHighSurrogate(text[i - 1])))
				return true;
		}

		return false;
	}
}
