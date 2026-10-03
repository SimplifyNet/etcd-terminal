namespace EtcdTerminal.Presentation;

/// <summary>
/// The one writer of a screen: it opens the viewport above the pinned footer,
/// streams blocks through it and redraws the footer in place.
/// </summary>
public interface IScreenCanvas
{
	void NewScreen(StatusBarModel footer);

	void Write(Block block);

	void Write(IReadOnlyList<Block> blocks);

	void UpdateFooter(StatusBarModel footer);

	void WriteException(Exception exception);
}
