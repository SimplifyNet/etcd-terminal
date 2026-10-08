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
public sealed class BackgroundBand(
	IRenderable _target,
	Color _background,
	bool _trailingBreak,
	Style? _stripeStyle = null,
	int _stripeOffset = 0,
	int _contentIndent = 0,
	IReadOnlyList<Color>? _lineBackgrounds = null,
	Color? _topPaddingBackground = null,
	bool _verticalPadding = true) : Renderable
{
	protected override Measurement Measure(RenderOptions options, int maxWidth) =>
		_target.Measure(options, maxWidth);

	protected override IEnumerable<Segment> Render(RenderOptions options, int maxWidth)
	{
		var stripeWidth = _stripeStyle is null ? 0 : 1;
		var targetWidth = Math.Max(0, maxWidth - _stripeOffset - stripeWidth - _contentIndent);
		var lines = Segment.SplitLines(_target.Render(options, targetWidth));
		List<Segment> result = [];

		if (_verticalPadding)
		{
			AddBandRow(result, [], maxWidth, _topPaddingBackground);
			result.Add(Segment.LineBreak);
		}

		for (var index = 0; index < lines.Count; index++)
		{
			var lineBackground = _lineBackgrounds is not null && index < _lineBackgrounds.Count
				? _lineBackgrounds[index]
				: _background;

			AddBandRow(result, lines[index], maxWidth, lineBackground);
			result.Add(Segment.LineBreak);
		}

		if (_verticalPadding)
			AddBandRow(result, [], maxWidth, _background);

		if (_trailingBreak)
			result.Add(Segment.LineBreak);

		return result;
	}

	private void AddBandRow(List<Segment> result, IReadOnlyList<Segment> line, int maxWidth, Color? rowBackground = null)
	{
		var background = rowBackground ?? _background;
		var backgroundStyle = new Style(background: background);
		var prefixWidth = _stripeOffset + (_stripeStyle is null ? 0 : 1) + _contentIndent;

		if (_stripeOffset > 0)
			result.Add(new Segment(new string(' ', _stripeOffset), backgroundStyle));

		if (_stripeStyle is { } stripeStyle)
			result.Add(new Segment("\u2503", new Style(stripeStyle.Foreground, background, stripeStyle.Decoration)));

		if (_contentIndent > 0)
			result.Add(new Segment(new string(' ', _contentIndent), backgroundStyle));

		foreach (var segment in line)
			result.Add(new Segment(segment.Text, new Style(segment.Style.Foreground, background, segment.Style.Decoration), segment.Link));

		var fill = maxWidth - prefixWidth - Segment.CellCount(line);

		if (fill > 0)
			result.Add(new Segment(new string(' ', fill), backgroundStyle));
	}
}
