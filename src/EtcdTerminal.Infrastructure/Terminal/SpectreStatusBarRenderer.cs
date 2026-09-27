using EtcdTerminal.Presentation;
using EtcdTerminal.Terminal;
using Spectre.Console;

namespace EtcdTerminal.Infrastructure.Terminal;

/// <summary>
/// Renders the footer as a borderless full-width block: session details on the
/// right, keyboard hints on the left. When the terminal is too narrow the model
/// is shortened in a fixed priority order so that a field is dropped before the
/// line is allowed to overflow.
/// </summary>
public sealed class SpectreStatusBarRenderer(RoleStyleMapper _styles) : IStatusBarRenderer
{
	private const int Indent = 2;

	/// The footer is a single row of text, but Spectre derives a panel's inner
	/// height from <c>Height - 2</c> because that rule is written for bordered
	/// panels. Pinning the height keeps one content row whatever region size the
	/// screen host allocates, so a one-row footer can never compute a negative
	/// height and break the whole frame.
	private const int PanelHeight = 3;
	private const int EndpointTruncationWidth = 20;
	private const int NameTruncationWidth = 24;
	private const int NameOverheadWidth = 4;
	private const string TruncationMark = "\u2026";

	public void Write(StatusBarModel model) =>
		AnsiConsole.Write(Build(Fit(model, AnsiConsole.Profile.Width)));

	public Panel Build(StatusBarModel model)
	{
		var grid = new Grid { Expand = true };

		grid.AddColumn(new GridColumn { Padding = new Padding(0, 0, 0, 0) });
		grid.AddColumn(new GridColumn { NoWrap = true, Alignment = Justify.Right });
		grid.AddRow(_styles.Build(model.Hints), _styles.Build(Right(model)));

		return new Panel(grid) { Height = PanelHeight }
			.NoBorder()
			.Padding(Indent, 0)
			.Expand();
	}

	/// <summary>
	/// Hints are the first thing to go, then the user, then the endpoint; the
	/// connection name is only shortened once nothing else is left. Returns the
	/// original model when it already fits.
	/// </summary>
	public static StatusBarModel Fit(StatusBarModel model, int width)
	{
		if (Fits(model, width) || model.Connection is null)
			return model;

		var shortened = model with { Connection = Truncate(model.Connection, EndpointTruncationWidth) };

		if (Fits(shortened, width))
			return shortened;

		var withoutUser = shortened with { Username = null };

		if (Fits(withoutUser, width))
			return withoutUser;

		var withoutEndpoint = withoutUser with { Connection = null };

		if (Fits(withoutEndpoint, width))
			return withoutEndpoint;

		var withoutHints = withoutEndpoint with { Hints = [] };

		if (Fits(withoutHints, width) || withoutHints.Name is null)
			return withoutHints;

		var budget = Math.Min(
			NameTruncationWidth,
			width - 2 * Indent - NameOverheadWidth - DisplayCells.Width(withoutHints.Version.Text));

		return withoutHints with { Name = Truncate(withoutHints.Name, budget) };
	}

	private static bool Fits(StatusBarModel model, int width) =>
		WidthOf(model.Hints) + RightWidth(model) + 2 * Indent <= width;

	private static int WidthOf(IReadOnlyList<StyledText> spans) =>
		spans.Sum(span => DisplayCells.Width(span.Text));

	private static int RightWidth(StatusBarModel model) => model.Name is null
		? 1 + DisplayCells.Width(model.Version.Text)
		: 2 + DisplayCells.Width(model.Name.Text)
			+ (model.Connection is null ? 0 : 3 + DisplayCells.Width(model.Connection.Text))
			+ (model.Username is null ? 0 : 3 + DisplayCells.Width(model.Username.Text))
			+ 2 + DisplayCells.Width(model.Version.Text);

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

	private static StyledText Truncate(StyledText span, int maxWidth)
	{
		if (DisplayCells.Width(span.Text) <= maxWidth)
			return span;

		var budget = maxWidth - DisplayCells.Width(TruncationMark);
		var width = 0;
		var length = 0;

		foreach (var rune in span.Text.EnumerateRunes())
		{
			var runeWidth = DisplayCells.Width(rune);

			if (width + runeWidth > budget)
				break;

			width += runeWidth;
			length += rune.Utf16SequenceLength;
		}

		return span with { Text = span.Text[..length] + TruncationMark };
	}
}
