namespace EtcdTerminal.Presentation;

/// <summary>
/// The one writer of a screen: it opens the viewport above the pinned footer,
/// streams blocks through it and redraws the footer in place. A page pins the
/// header and the hint and clips the body to the rows between them, so the
/// body can be shown at another offset without losing the top of the screen.
/// </summary>
public interface IScreenCanvas
{
	void NewScreen(StatusBarModel footer);

	void OpenPage(StatusBarModel footer, IReadOnlyList<Block> header, IReadOnlyList<Block> body, IReadOnlyList<Block> pinned);

	/// Moves a page's body by a step. False when there is no page or the
	/// offset did not change, so a wheel that has nothing to scroll stays
	/// silent instead of continuing the screen.
	bool Scroll(ScrollStep step);

	void Write(Block block);

	void Write(IReadOnlyList<Block> blocks);

	void UpdateFooter(StatusBarModel footer);

	void WriteException(Exception exception);

	/// Draws the current screen again for the current terminal size.
	void Redraw();
}
