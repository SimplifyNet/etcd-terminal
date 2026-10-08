using EtcdTerminal.Presentation;

namespace EtcdTerminal.App.Components;

/// <summary>
/// Opens a screen on the canvas: the banner, the body the caller composed and
/// the session footer. Every later write goes through the same canvas, so one
/// component owns every row of the viewport.
/// </summary>
public sealed class Screen(IScreenCanvas _canvas, Header _header, StatusBar _statusBar)
{
	public void Open(IReadOnlyList<Block>? body = null)
	{
		_canvas.NewScreen(_statusBar.BuildModel());
		_canvas.Write(_header.BuildModel());

		if (body is not null)
			_canvas.Write(body);
	}

	/// Opens the screen as a page: the banner and the pinned blocks stay
	/// fixed and the body is clipped to the rows between them, reachable by
	/// scrolling.
	public void OpenPage(IReadOnlyList<Block> body, IReadOnlyList<Block> pinned) =>
		_canvas.OpenPage(_statusBar.BuildModel(), [_header.BuildModel()], body, pinned);

	public BannerBlock Banner() => _header.BuildModel();

	public void Reset() => _canvas.NewScreen(_statusBar.BuildModel());

	public bool Scroll(ScrollStep step) => _canvas.Scroll(step);

	public void Write(Block block) => _canvas.Write(block);

	public void Write(IReadOnlyList<Block> blocks) => _canvas.Write(blocks);
}
