namespace EtcdTerminal.Presentation;

/// <summary>
/// One screen frame: a header, an ordered body and a footer. Describes logical
/// regions only. Sizes, borders, padding, overflow policy and where the frame
/// lives on screen belong to Infrastructure.
/// </summary>
public sealed record ScreenModel
{
	/// <summary>
	/// Banner shown above the body, or null for screens without one.
	/// </summary>
	public BannerBlock? Header { get; init; }

	/// <summary>
	/// Content of the screen, in order.
	/// </summary>
	public IReadOnlyList<Block> Body { get; init; } = [];

	/// <summary>
	/// Session information pinned below the body. Null hides the footer.
	/// </summary>
	public StatusBarModel? Footer { get; init; }
}
