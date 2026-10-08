using Spectre.Console;
using Spectre.Console.Rendering;

namespace EtcdTerminal.Infrastructure.Terminal;

/// <summary>
/// The band treatment shared by the status bar, the key filter line and the
/// pagination line: a row of the band background above the target, the target
/// itself and a row of background below it, every row stretched edge to edge
/// with the target's styles carried over on top of the background. The
/// trailing line break is what lets the next body block follow; the
/// bottom-anchored status bar drops it, because written on the last terminal
/// row a break scrolls the whole screen by one. Spectre's Padder cannot do
/// this job: Segment.Padding builds an unstyled space, so its fill carries no
/// background, and it emits a line break after the bottom padding, so it
/// cannot close without scrolling either.
/// </summary>
public sealed class BackgroundBand(IRenderable _target, BandStyle _style) : Renderable
{
	protected override Measurement Measure(RenderOptions options, int maxWidth) =>
		_target.Measure(options, maxWidth);

	protected override IEnumerable<Segment> Render(RenderOptions options, int maxWidth)
	{
		var stripeWidth = _style.StripeStyle is null ? 0 : 1;
		var targetWidth = Math.Max(0, maxWidth - _style.StripeOffset - stripeWidth - _style.ContentIndent);
		var lines = Segment.SplitLines(_target.Render(options, targetWidth));
		List<Segment> result = [];

		if (_style.VerticalPadding)
		{
			AddBandRow(result, [], maxWidth, _style.TopPaddingBackground);
			result.Add(Segment.LineBreak);
		}

		for (var index = 0; index < lines.Count; index++)
		{
			var lineBackground = _style.LineBackgrounds is not null && index < _style.LineBackgrounds.Count
				? _style.LineBackgrounds[index]
				: _style.Background;

			AddBandRow(result, lines[index], maxWidth, lineBackground);
			result.Add(Segment.LineBreak);
		}

		if (_style.VerticalPadding)
			AddBandRow(result, [], maxWidth, _style.Background);

		if (_style.TrailingBreak)
			result.Add(Segment.LineBreak);

		return result;
	}

	private void AddBandRow(List<Segment> result, IReadOnlyList<Segment> line, int maxWidth, Color? rowBackground = null)
	{
		var background = rowBackground ?? _style.Background;
		var backgroundStyle = new Style(background: background);
		var prefixWidth = _style.StripeOffset + (_style.StripeStyle is null ? 0 : 1) + _style.ContentIndent;

		if (_style.StripeOffset > 0)
			result.Add(new Segment(new string(' ', _style.StripeOffset), backgroundStyle));

		if (_style.StripeStyle is { } stripeStyle)
			result.Add(new Segment("\u2503", new Style(stripeStyle.Foreground, background, stripeStyle.Decoration)));

		if (_style.ContentIndent > 0)
			result.Add(new Segment(new string(' ', _style.ContentIndent), backgroundStyle));

		foreach (var segment in line)
			result.Add(new Segment(segment.Text, new Style(segment.Style.Foreground, background, segment.Style.Decoration), segment.Link));

		var fill = maxWidth - prefixWidth - Segment.CellCount(line);

		if (fill > 0)
			result.Add(new Segment(new string(' ', fill), backgroundStyle));
	}
}
