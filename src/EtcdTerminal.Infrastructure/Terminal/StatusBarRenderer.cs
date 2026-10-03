using EtcdTerminal.Presentation;
using Spectre.Console;
using Spectre.Console.Rendering;

namespace EtcdTerminal.Infrastructure.Terminal;

/// <summary>
/// Builds the footer renderable: keyboard hints on the left, session details on
/// the right. The bar has exactly one row: the screen host gives it one row and
/// a second line would land on the last terminal row, where a line break scrolls
/// the whole screen. Which fields survive a narrow terminal is decided by asking
/// Spectre to render each candidate and count its lines, in the priority order
/// the footer had before the migration.
/// </summary>
public sealed class StatusBarRenderer(IAnsiConsole _console, RoleStyleMapper _styles)
{
	/// <summary>
	/// An endpoint shorter than this is not worth a place in the footer: it is
	/// dropped instead of being whittled down to a fragment.
	/// </summary>
	private const int EndpointTruncationWidth = 20;

	/// <summary>
	/// Appended to an endpoint that is being shortened field by field.
	/// </summary>
	private const string TruncationMark = "\u2026";

	public IRenderable Build(StatusBarModel model) => new BottomLine(Bar(Fit(model)));

	/// <summary>
	/// The first candidate Spectre can put on a single row. Candidates are tried
	/// in the order the footer used to drop them: shorten the endpoint, then the
	/// user, then the endpoint itself, then the hints, then the connection name.
	/// </summary>
	private StatusBarModel Fit(StatusBarModel model)
	{
		var candidates = Candidates(model).ToList();

		foreach (var candidate in candidates)
			if (Fits(candidate))
				return candidate;

		return candidates[^1];
	}

	/// <summary>
	/// Spectre decides, not this class: the footer is rendered at the console
	/// width and a renderable that comes back as more than one line does not fit.
	/// </summary>
	private bool Fits(StatusBarModel model)
	{
		IRenderable footer = new BottomLine(Bar(model));

		return Segment.SplitLines(footer.Render(RenderOptions.Create(_console), _console.Profile.Width)).Count <= 1;
	}

	private Grid Bar(StatusBarModel model)
	{
		var grid = new Grid { Expand = true };

		grid.AddColumn(new GridColumn { NoWrap = true, Padding = new Padding(2, 0, 0, 0) });
		grid.AddColumn(new GridColumn { NoWrap = true, Alignment = Justify.Right, Padding = new Padding(0, 0, 2, 0) });

		var left = _styles.Build(model.Hints);
		var right = _styles.Build(Right(model));

		left.Overflow = Overflow.Crop;
		right.Overflow = Overflow.Ellipsis;

		grid.AddRow(left, right);

		return grid;
	}

	private static IEnumerable<StatusBarModel> Candidates(StatusBarModel model)
	{
		yield return model;

		foreach (var shortened in Shorter(model.Connection))
			yield return model with { Connection = shortened };

		yield return model with { Username = null };

		foreach (var shortened in Shorter(model.Connection))
			yield return model with { Connection = shortened, Username = null };

		yield return model with { Connection = null, Username = null };
		yield return model with { Connection = null, Username = null, Hints = [] };
		yield return model with { Connection = null, Username = null, Hints = [], Name = null };
	}

	private static IEnumerable<StyledText> Shorter(StyledText? connection)
	{
		if (connection is null)
			yield break;

		var text = connection.Text;
		var cut = text.Length;

		while (cut > 0)
		{
			cut--;

			if (cut > 0 && char.IsLowSurrogate(text[cut]))
				cut--;

			var shortened = cut + TruncationMark.Length;

			if (shortened < EndpointTruncationWidth)
				yield break;

			if (shortened < text.Length)
				yield return connection with { Text = string.Concat(text.AsSpan(0, cut), TruncationMark) };
		}
	}

	private static List<StyledText> Right(StatusBarModel model)
	{
		List<StyledText> spans = [];

		if (model.Name is null)
		{
			spans.Add(new StyledText("v", TextRole.Muted));
			spans.Add(model.Version);

			return spans;
		}

		spans.Add(new StyledText("\u2022", TextRole.Success));
		spans.Add(Spaced(model.Name, TextRole.Secondary));

		if (model.Connection is not null)
		{
			spans.Add(new StyledText(" \u00b7", TextRole.Subtle));
			spans.Add(Spaced(model.Connection, TextRole.Muted));
		}

		if (model.Username is not null)
		{
			spans.Add(new StyledText(" \u00b7", TextRole.Subtle));
			spans.Add(Spaced(model.Username, TextRole.Warning));
		}

		spans.Add(new StyledText(" v", TextRole.Muted));
		spans.Add(model.Version);

		return spans;
	}

	private static StyledText Spaced(StyledText span, TextRole role) =>
		new(" " + span.Text, role);

	/// <summary>
	/// A grid ends with a line break, which is what a parent layout needs and
	/// what a bottom anchored footer must not get: written on the last row of
	/// the terminal that break scrolls the whole screen by one. Spectre has no
	/// renderable that drops it, so the footer renders itself without it.
	/// </summary>
	private sealed class BottomLine(IRenderable _target) : Renderable
	{
		protected override Measurement Measure(RenderOptions options, int maxWidth) =>
			_target.Measure(options, maxWidth);

		protected override IEnumerable<Segment> Render(RenderOptions options, int maxWidth)
		{
			var segments = new List<Segment>(_target.Render(options, maxWidth));

			while (segments.Count > 0 && segments[^1].IsLineBreak)
				segments.RemoveAt(segments.Count - 1);

			return segments;
		}
	}
}
