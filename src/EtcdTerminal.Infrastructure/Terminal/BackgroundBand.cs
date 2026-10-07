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
public sealed class BackgroundBand(IRenderable _target, Color _background, bool _trailingBreak) : Renderable
{
	protected override Measurement Measure(RenderOptions options, int maxWidth) =>
		_target.Measure(options, maxWidth);

	protected override IEnumerable<Segment> Render(RenderOptions options, int maxWidth)
	{
		var lines = Segment.SplitLines(_target.Render(options, maxWidth));
		var pad = new Segment(new string(' ', maxWidth), new Style(background: _background));
		List<Segment> result = [pad, Segment.LineBreak];

		foreach (var line in lines)
		{
			var fill = maxWidth - Segment.CellCount(line);

			foreach (var segment in line)
				result.Add(new Segment(segment.Text, new Style(segment.Style.Foreground, _background, segment.Style.Decoration), segment.Link));

			if (fill > 0)
				result.Add(new Segment(new string(' ', fill), new Style(background: _background)));

			result.Add(Segment.LineBreak);
		}

		result.Add(pad);

		if (_trailingBreak)
			result.Add(Segment.LineBreak);

		return result;
	}
}
