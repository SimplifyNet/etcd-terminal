using Spectre.Console;

namespace EtcdTerminal.Infrastructure.Terminal;

/// <summary>
/// A spinner whose frames carry the margin of a status line: the status widget
/// places its spinner column on the left edge, so the four columns of
/// <see cref="ContentIndent"/> are prefixed to every frame instead of to the
/// message, which the widget trims before it paints.
/// </summary>
public sealed class IndentedSpinner(Spinner _inner) : Spinner
{
	public override TimeSpan Interval => _inner.Interval;

	public override bool IsUnicode => _inner.IsUnicode;

	public override IReadOnlyList<string> Frames { get; } =
		[.. _inner.Frames.Select(frame => ContentIndent.Text + frame)];
}
