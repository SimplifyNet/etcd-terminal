using Spectre.Console;

namespace EtcdTerminal.Infrastructure.Terminal;

public sealed class CustomSpinner : Spinner
{
	private const string Glyphs = "⣷⣯⣟⡿⢿⣻⣽⣾";

	public override TimeSpan Interval => TimeSpan.FromMilliseconds(100);

	public override bool IsUnicode => true;

	public override IReadOnlyList<string> Frames { get; } =
		[.. Glyphs.Select(glyph => ContentIndent.Marker + glyph)];
}
