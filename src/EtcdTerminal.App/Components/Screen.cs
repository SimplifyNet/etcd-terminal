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

	public void Write(Block block) => _canvas.Write(block);

	public void Write(IReadOnlyList<Block> blocks) => _canvas.Write(blocks);
}
