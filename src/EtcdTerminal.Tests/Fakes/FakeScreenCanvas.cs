using EtcdTerminal.Presentation;

namespace EtcdTerminal.Tests.Fakes;

/// <summary>
/// Records what a screen asked the canvas to do instead of drawing it. Lets a
/// test assert the footer model, the blocks a component opened and the scroll
/// steps it forwarded without depending on terminal geometry.
/// </summary>
public sealed class FakeScreenCanvas : IScreenCanvas
{
	public List<StatusBarModel> Footers { get; } = [];

	public List<Block> Blocks { get; } = [];

	public List<ScrollStep> Scrolls { get; } = [];

	public bool ScrollResult { get; set; }

	public int NewScreenCount { get; private set; }

	public void NewScreen(StatusBarModel footer)
	{
		Footers.Add(footer);
		NewScreenCount++;
	}

	public void OpenPage(StatusBarModel footer, IReadOnlyList<Block> header, IReadOnlyList<Block> body, IReadOnlyList<Block> pinned)
	{
		Footers.Add(footer);
		Write(header);
		Write(body);
		Write(pinned);
	}

	public bool Scroll(ScrollStep step)
	{
		Scrolls.Add(step);
		return ScrollResult;
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
