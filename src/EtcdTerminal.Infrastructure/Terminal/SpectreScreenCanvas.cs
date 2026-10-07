using EtcdTerminal.Presentation;
using Spectre.Console;
using Spectre.Console.Rendering;

namespace EtcdTerminal.Infrastructure.Terminal;

/// <summary>
/// The one writer of a screen while a session runs. The scroll region and the
/// footer rows were reserved by <see cref="ConsoleTerminalSession"/>, so a
/// screen is opened by erasing the viewport, blocks stream through it and the
/// footer is redrawn in place on the rows the scroll region never touches.
/// A page additionally pins the header and the hint and clips the body to the
/// rows between them: Spectre's Live only crops a renderable to the screen,
/// it cannot show a taller body at another offset, and that gap is what the
/// window and the redraw close.
/// </summary>
public sealed class SpectreScreenCanvas(IAnsiConsole _console, BlockRenderer _blocks, StatusBarRenderer _footer) : IScreenCanvas
{
	/// The footer owns the last three rows. The row above them is a guard:
	/// every part of a page is written with a trailing line break, and a
	/// break issued on the last row of the scroll region would scroll the
	/// region — taking the header with it. The guard row absorbs that break.
	private const int FooterRows = 3;
	private const int GuardRows = 1;

	private IRenderable? _pageHeader;
	private IRenderable? _pageBody;
	private IRenderable? _pagePinned;
	private StatusBarModel? _pageFooter;
	private int _pageTotal;
	private int _pageViewport;
	private int _pageOffset;
	private bool _pageActive;

	public void NewScreen(StatusBarModel footer)
	{
		_pageActive = false;

		_console.Clear(true);

		if (_console.Profile.Capabilities.Ansi)
			UpdateFooter(footer);
		else
			_console.Write(_footer.Build(footer));
	}

	public void OpenPage(StatusBarModel footer, IReadOnlyList<Block> header, IReadOnlyList<Block> body, IReadOnlyList<Block> pinned)
	{
		var headerRender = new Rows(header.Select(_blocks.Render));
		var pinnedRender = new Rows(pinned.Select(_blocks.Render));
		var bodyRender = new Rows(body.Select(_blocks.Render));

		_pageHeader = headerRender;
		_pagePinned = pinnedRender;
		_pageBody = bodyRender;
		_pageFooter = footer;
		_pageOffset = 0;

		if (_console.Profile.Capabilities.Ansi)
		{
			_pageTotal = Lines(bodyRender);
			_pageViewport = _console.Profile.Height - FooterRows - GuardRows - Lines(headerRender) - Lines(pinnedRender);
			_pageActive = _pageViewport > 0;

			if (_pageActive)
			{
				DrawPage();

				return;
			}
		}

		// No ANSI or a terminal too small for a window: stream as a plain
		// screen does, losing only the scrollability.
		NewScreen(footer);
		_console.Write(headerRender);
		_console.Write(bodyRender);
		_console.Write(pinnedRender);
	}

	public bool Scroll(ScrollStep step)
	{
		if (!_pageActive || _pageBody is null)
			return false;

		var last = Math.Max(0, _pageTotal - _pageViewport);

		var next = step switch
		{
			ScrollStep.LineUp => _pageOffset - 1,
			ScrollStep.LineDown => _pageOffset + 1,
			ScrollStep.PageUp => _pageOffset - _pageViewport,
			ScrollStep.PageDown => _pageOffset + _pageViewport,
			ScrollStep.Top => 0,
			ScrollStep.Bottom => last,
			_ => _pageOffset
		};

		next = Math.Clamp(next, 0, last);

		if (next == _pageOffset)
			return false;

		_pageOffset = next;
		DrawPage();

		return true;
	}

	public void Write(Block block) => _console.Write(_blocks.Render(block));

	public void Write(IReadOnlyList<Block> blocks)
	{
		foreach (var block in blocks)
			Write(block);
	}

	public void UpdateFooter(StatusBarModel footer)
	{
		var renderable = _footer.Build(footer);

		_console.WriteAnsi(writer => writer.SaveCursor(false).CursorPosition(_console.Profile.Height - FooterRows + 1, 1));
		_console.Write(renderable);
		_console.WriteAnsi(writer => writer.RestoreCursor(false));
	}

	public void WriteException(Exception exception) => _console.WriteException(exception);

	/// Redraws the whole page — a clear, the pinned header, the window of the
	/// body at the current offset, the pinned hint and the footer. Redrawing
	/// from scratch is what keeps every part on its own rows whatever the
	/// previous draw left behind.
	private void DrawPage()
	{
		_console.Clear(true);
		_console.Write(_pageHeader!);
		_console.Write(new LineWindow(_pageBody!, _pageOffset, _pageViewport));
		_console.Write(_pagePinned!);
		UpdateFooter(_pageFooter!);
	}

	private int Lines(IRenderable renderable) =>
		Segment.SplitLines(renderable.Render(RenderOptions.Create(_console), _console.Profile.Width)).Count;

	/// The window's lines, each followed by a line break so the cursor lands
	/// on the next row whatever the widget inside does with its own trailing
	/// break. A window shorter than the viewport is padded, which erases what
	/// the previous draw left on those rows.
	private sealed class LineWindow(IRenderable _inner, int _skip, int _take) : Renderable
	{
		protected override Measurement Measure(RenderOptions options, int maxWidth) => _inner.Measure(options, maxWidth);

		protected override IEnumerable<Segment> Render(RenderOptions options, int maxWidth)
		{
			var lines = Segment.SplitLines(_inner.Render(options, maxWidth));
			var result = new List<Segment>();

			for (var index = _skip; index < _skip + _take; index++)
			{
				if (index < lines.Count)
					result.AddRange(lines[index]);

				result.Add(Segment.LineBreak);
			}

			return result;
		}
	}
}
