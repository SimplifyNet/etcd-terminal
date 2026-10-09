using EtcdTerminal.Presentation;
using EtcdTerminal.Presentation.Theming;
using Spectre.Console;
using Spectre.Console.Rendering;

namespace EtcdTerminal.Infrastructure.Terminal;

/// <summary>
/// Builds the footer renderable: keyboard hints on the left, session details
/// on the right, painted as a three-row band (see <see cref="BackgroundBand"/>)
/// with the band background edge to edge. The text stays on one row: a second
/// text line would land on the last terminal row, where a line break scrolls
/// the whole screen. Which fields survive a narrow terminal is decided by
/// asking Spectre to render each candidate from <see cref="StatusBarFallbacks"/>
/// and count its lines.
/// </summary>
public sealed class StatusBarRenderer(IAnsiConsole _console, RoleStyleMapper _styles, IThemeCatalog _themes)
{
	public IRenderable Build(StatusBarModel model)
	{
		var background = new Color(_themes.Current.BandBackground.R, _themes.Current.BandBackground.G, _themes.Current.BandBackground.B);

		return new BackgroundBand(Bar(Fit(model)), new BandStyle(background, TrailingBreak: false));
	}

	/// <summary>
	/// The first candidate Spectre can put on a single row, in the drop order
	/// the presentation chose.
	/// </summary>
	private StatusBarModel Fit(StatusBarModel model)
	{
		var candidates = StatusBarFallbacks.InPriorityOrder(model).ToList();

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
		IRenderable footer = Bar(model);

		return Segment.SplitLines(footer.Render(RenderOptions.Create(_console), _console.Profile.Width)).Count <= 1;
	}

	private Grid Bar(StatusBarModel model)
	{
		var grid = new Grid { Expand = true };

		grid.AddColumn(new GridColumn { NoWrap = true, Padding = new Padding(ContentIndent.BandColumns, 0, 0, 0) });
		grid.AddColumn(new GridColumn { NoWrap = true, Alignment = Justify.Right, Padding = new Padding(0, 0, ContentIndent.BandColumns, 0) });

		var left = _styles.Build(model.Hints);
		var right = _styles.Build(Right(model));

		left.Overflow = Overflow.Crop;
		right.Overflow = Overflow.Ellipsis;

		grid.AddRow(left, right);

		return grid;
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
}
