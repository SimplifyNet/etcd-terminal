using EtcdTerminal.Presentation;

namespace EtcdTerminal.Tests.Fakes;

/// <summary>
/// Records what a screen asked the canvas to do instead of drawing it. Lets a
/// test assert the footer model and the blocks a component streamed without
/// depending on terminal geometry.
/// </summary>
public sealed class FakeScreenCanvas : IScreenCanvas
{
	public List<StatusBarModel> Footers { get; } = [];

	public List<Block> Blocks { get; } = [];

	public int NewScreenCount { get; private set; }

	public void NewScreen(StatusBarModel footer)
	{
		Footers.Add(footer);
		NewScreenCount++;
	}

	public void Write(Block block) => Blocks.Add(block);

	public void Write(IReadOnlyList<Block> blocks)
	{
		foreach (var block in blocks)
			Write(block);
	}

	public void UpdateFooter(StatusBarModel footer) => Footers.Add(footer);

	public void WriteException(Exception exception) =>
		Write(TextBlock.Line(new StyledText(exception.Message, TextRole.Danger)));
}
