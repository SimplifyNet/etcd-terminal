using Spectre.Console;

namespace EtcdTerminal.Infrastructure.Terminal;

/// <summary>
/// The band treatment a <see cref="BackgroundBand"/> applies: the row
/// background every row is filled with, and the optional stripe, indent,
/// per-line backgrounds and padding rows layered on top of it.
/// </summary>
public sealed record BandStyle(Color Background, bool TrailingBreak)
{
	public Style? StripeStyle { get; init; }
	public int StripeOffset { get; init; }
	public int ContentIndent { get; init; }
	public IReadOnlyList<Color>? LineBackgrounds { get; init; }
	public Color? TopPaddingBackground { get; init; }
	public bool VerticalPadding { get; init; } = true;
}
