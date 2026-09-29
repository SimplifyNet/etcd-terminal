using EtcdTerminal.Terminal;
using EtcdTerminal.Tests.Fakes;
using NUnit.Framework;

namespace EtcdTerminal.Tests;

[TestFixture]
public sealed class RecordingTerminalTests
{
	[Test]
	public void Markers_CostNoCells()
	{
		var terminal = new RecordingTerminal();

		Assert.That(terminal.GetVisibleLength("<accent>hi</>"), Is.EqualTo(2));
		Assert.That(terminal.GetVisibleLength("<panel><muted>  </muted></>"), Is.EqualTo(2));
	}

	[Test]
	public void Write_AdvancesCursorByCells()
	{
		var terminal = new RecordingTerminal();

		terminal.Write("ab\ncd");
		terminal.Write("<accent>e</>");

		Assert.That((terminal.CursorLeft, terminal.CursorTop), Is.EqualTo((3, 1)));
		Assert.That(terminal.Writes.Count, Is.EqualTo(2));
		Assert.That(terminal.Writes[0], Is.EqualTo((0, 0, "ab\ncd")));
	}

	[Test]
	public void WriteLine_MovesToNextRow()
	{
		var terminal = new RecordingTerminal();

		terminal.WriteLine("hi");

		Assert.That((terminal.CursorLeft, terminal.CursorTop), Is.EqualTo((0, 1)));
	}

	[Test]
	public void SetCursorPosition_IsRecorded()
	{
		var terminal = new RecordingTerminal();

		terminal.SetCursorPosition(3, 7);

		Assert.That(terminal.CursorSets, Is.EqualTo([(3, 7)]));
		Assert.That((terminal.CursorLeft, terminal.CursorTop), Is.EqualTo((3, 7)));
	}

	[Test]
	public void Dimensions_AreConfigurable()
	{
		var terminal = new RecordingTerminal { WindowWidth = 40, WindowHeight = 10 };

		Assert.That(terminal.WindowWidth, Is.EqualTo(40));
		Assert.That(terminal.WindowHeight, Is.EqualTo(10));
	}
}
