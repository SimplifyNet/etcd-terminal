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
///
/// What the screen wrote is kept until the next screen opens, so a change of
/// the window size redraws the same screen for the new size.
/// </summary>
public sealed class SpectreScreenCanvas(IAnsiConsole _console, BlockRenderer _blocks, StatusBarRenderer _footer) : IScreenCanvas
{
	/// The footer owns the last three rows. The row above them is a guard:
	/// every part of a page is written with a trailing line break, and a
	/// break issued on the last row of the scroll region would scroll the
	/// region — taking the header with it. The guard row absorbs that break.
	private const int FooterRows = 3;
	private const int GuardRows = 1;

	/// A resize redraws from the session's timer thread while a screen may be
	/// writing from its own; both go through here one at a time.
	private readonly Lock _gate = new();
	private readonly List<Func<IRenderable>> _written = [];

	private StatusBarModel? _footerModel;
	private IReadOnlyList<Block> _pageHeaderBlocks = [];
	private IReadOnlyList<Block> _pageBodyBlocks = [];
	private IReadOnlyList<Block> _pagePinnedBlocks = [];
	private IRenderable? _pageHeader;
	private IRenderable? _pageBody;
	private IRenderable? _pagePinned;
	private int _pageTotal;
	private int _pageViewport;
	private int _pageOffset;
	private bool _pageActive;
	private bool _pageOpen;

	public void NewScreen(StatusBarModel footer)
	{
		lock (_gate)
		{
			_pageActive = false;
			_pageOpen = false;
			_footerModel = footer;

			_written.Clear();
			_console.Clear(true);

			if (_console.Profile.Capabilities.Ansi)
				UpdateFooter(footer);
			else
				_console.Write(_footer.Build(footer));
		}
	}

	public void OpenPage(StatusBarModel footer, IReadOnlyList<Block> header, IReadOnlyList<Block> body, IReadOnlyList<Block> pinned)
	{
		lock (_gate)
		{
			_pageHeaderBlocks = header;
			_pagePinnedBlocks = pinned;
			_pageBodyBlocks = body;
			_footerModel = footer;
			_pageOffset = 0;

			_written.Clear();
			DrawPageOrStream();
		}
	}

	public bool Scroll(ScrollStep step)
	{
		lock (_gate)
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
	}

	public void Write(Block block)
	{
		lock (_gate)
			Write(() => _blocks.Render(block));
	}

	public void Write(IReadOnlyList<Block> blocks)
	{
		foreach (var block in blocks)
			Write(block);
	}

	/// One write for the whole footer: an active Live (a menu) appends its
	/// region after every write, so the cursor has to be back in the viewport
	/// before that write ends.
	public void UpdateFooter(StatusBarModel footer)
	{
		lock (_gate)
		{
			_footerModel = footer;

			_console.Write(new Pinned(_footer.Build(footer), _console.Profile.Height - FooterRows + 1, _console.Profile.Capabilities));
		}
	}

	public void WriteException(Exception exception)
	{
		lock (_gate)
		{
			var renderable = exception.GetRenderable();

			Write(() => renderable);
		}
	}

	/// The current screen again, laid out for the new window size: a page
	/// recomputes its window, a plain screen replays what it streamed, and an
	/// active Live redraws its region after the footer write.
	public void Redraw()
	{
		lock (_gate)
		{
			if (!_console.Profile.Capabilities.Ansi || _footerModel is null)
				return;

			if (_pageOpen)
			{
				DrawPageOrStream();

				return;
			}

			var written = _written.ToList();

			NewScreen(_footerModel);

			foreach (var renderable in written)
				Write(renderable);
		}
	}

	/// What a screen wrote is kept as the way to build it, not as the built
	/// widget, so a redraw after a theme switch picks up the new colors.
	private void Write(Func<IRenderable> build)
	{
		_written.Add(build);
		_console.Write(build());
	}

	private void DrawPageOrStream()
	{
		_pageHeader = new Rows(_pageHeaderBlocks.Select(_blocks.Render));
		_pagePinned = new Rows(_pagePinnedBlocks.Select(_blocks.Render));
		_pageBody = new Rows(_pageBodyBlocks.Select(_blocks.Render));

		if (_console.Profile.Capabilities.Ansi)
		{
			_pageTotal = Lines(_pageBody!);
			_pageViewport = _console.Profile.Height - FooterRows - GuardRows - Lines(_pageHeader!) - Lines(_pagePinned!);
			_pageActive = _pageViewport > 0;

			if (_pageActive)
			{
				_pageOffset = Math.Clamp(_pageOffset, 0, Math.Max(0, _pageTotal - _pageViewport));
				_pageOpen = true;

				DrawPage();

				return;
			}
		}

		// No ANSI or a terminal too small for a window: stream as a plain
		// screen does, losing only the scrollability.
		NewScreen(_footerModel!);
		var header = _pageHeaderBlocks;
		var body = _pageBodyBlocks;
		var pinned = _pagePinnedBlocks;

		Write(() => new Rows(header.Select(_blocks.Render)));
		Write(() => new Rows(body.Select(_blocks.Render)));
		Write(() => new Rows(pinned.Select(_blocks.Render)));
		_pageOpen = true;
	}

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
		UpdateFooter(_footerModel!);
	}

	private int Lines(IRenderable renderable) =>
		Segment.SplitLines(renderable.Render(RenderOptions.Create(_console), _console.Profile.Width)).Count;

	/// The inner renderable drawn on the given row, the cursor saved before
	/// and restored after, all within one renderable.
	private sealed class Pinned(IRenderable _inner, int _row, AnsiCapabilities _capabilities) : Renderable
	{
		protected override Measurement Measure(RenderOptions options, int maxWidth) => _inner.Measure(options, maxWidth);

		protected override IEnumerable<Segment> Render(RenderOptions options, int maxWidth) =>
		[
			Control(writer => writer.SaveCursor(false).CursorPosition(_row, 1)),
			.. _inner.Render(options, maxWidth),
			Control(writer => writer.RestoreCursor(false))
		];

		private Segment Control(Action<AnsiWriter> write)
		{
			var buffer = new StringWriter();

			write(new AnsiWriter(buffer, _capabilities));

			return Segment.Control(buffer.ToString());
		}
	}

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
