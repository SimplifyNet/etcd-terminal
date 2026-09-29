using EtcdTerminal.Presentation;
using Spectre.Console;
using Spectre.Console.Rendering;

namespace EtcdTerminal.Infrastructure.Terminal;

/// <summary>
/// The single owner of the console for one screen. The frame is started at the
/// top row, every update replaces it instead of appending, and the footer region
/// has a fixed height so it stays on the last row of the terminal.
/// </summary>
public sealed class SpectreScreenHost(IAnsiConsole _console, SpectrePanelRenderer _panels, SpectreStatusBarRenderer _footer) : IScreenHost
{
	/// The status bar is a single line pinned to the last row of the terminal.
	private const int FooterRows = 1;

	/// Rows kept free below a released frame so the screen can stream a result
	/// without scrolling the frame away: blank, up to three text lines, blank
	/// and the press-any-key label.
	private const int StreamReserve = 7;

	private readonly SemaphoreSlim _updates = new(0);
	private TaskCompletionSource _painted = new(TaskCreationOptions.RunContinuationsAsynchronously);
	private readonly Lock _gate = new();

	private ScreenModel? _model;
	private Task? _pump;
	private bool _closing;

	public void Begin(ScreenModel model)
	{
		lock (_gate)
		{
			ArgumentNullException.ThrowIfNull(model);

			if (_pump is not null)
				throw new InvalidOperationException("The screen host is already running.");

			_model = model;
			_closing = false;
			_painted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
			_pump = Task.Run(PumpAsync);
		}

		// The first paint either happens or the pump fails. Waiting only on the
		// paint would hang forever when rendering throws inside the live display.
		if (Task.WaitAny(_painted.Task, _pump) == 1)
			_pump.GetAwaiter().GetResult();
	}

	public void Update(ScreenModel model)
	{
		lock (_gate)
		{
			ArgumentNullException.ThrowIfNull(model);

			if (_pump is null)
				throw new InvalidOperationException("The screen host has not been started.");

			_model = model;
		}

		_updates.Release();
	}

	public void End()
	{
		Task? pump;

		lock (_gate)
		{
			pump = _pump;
			_pump = null;
			_closing = true;
		}

		if (pump is null)
			return;

		_updates.Release();
		pump.GetAwaiter().GetResult();
		Paint();

		// Spectre shows the cursor when its live display completes. A screen
		// that streams its own output afterwards must start from the hidden
		// cursor the terminal was initialized with; prompts that need the caret
		// show it themselves.
		_console.Cursor.Hide();
	}

	/// <summary>
	/// Leaves the released frame on the screen and parks the cursor where the
	/// screen can stream its result, so output such as a prompt continues below
	/// the frame instead of over the status bar on the last row.
	/// </summary>
	private void Paint()
	{
		ScreenModel? model;

		lock (_gate)
			model = _model;

		if (model is null)
			return;

		// The live display erased its region and left the cursor wherever its own
		// bookkeeping put it. The frame always starts at the top row, so anchoring
		// there before writing keeps a full height frame from running past the last
		// row and scrolling its footer back on screen.
		SpectreCursorPosition.MoveTo(_console.Cursor, _console.Profile.Capabilities.Ansi, 0, 0);

		_console.Write(Frame(model));
		SpectreCursorPosition.MoveTo(_console.Cursor, _console.Profile.Capabilities.Ansi, 0, HandOverRow(model));
	}

	/// The last row of the frame's content that still leaves room for the output
	/// the screen streams after the frame is released. A message, its trailing
	/// blank line and the press-any-key label must fit above the footer: when
	/// they do not, the terminal scrolls and the frame's own footer comes back
	/// on screen a second time.
	private int HandOverRow(ScreenModel model)
	{
		var lines = Segment.SplitLines(Main(model).Render(RenderOptions.Create(_console), _console.Profile.Width));
		var lastRow = Math.Max(0, _console.Profile.Height - FooterRows - 1 - StreamReserve);

		return Math.Min(lines.Count, lastRow);
	}

	private async Task PumpAsync()
	{
		ScreenModel? model;

		lock (_gate)
			model = _model;

		_console.Clear(true);

		_console.Live(Frame(model!))
			.Overflow(VerticalOverflow.Crop)
			.Cropping(VerticalOverflowCropping.Bottom)
			.AutoClear(true)
			.StartAsync(PumpAsync)
			.GetAwaiter()
			.GetResult();
	}

	private async Task PumpAsync(LiveDisplayContext ctx)
	{
		ctx.Refresh();
		_painted.TrySetResult();

		while (true)
		{
			await _updates.WaitAsync().ConfigureAwait(false);

			ScreenModel? next;

			lock (_gate)
			{
				if (_closing)
					break;

				next = _model;
			}

			ctx.UpdateTarget(Frame(next!));
		}
	}

	private Layout Frame(ScreenModel model)
	{
		var layout = new Layout();

		layout.SplitRows(
			new Layout(MainRegion),
			new Layout(FooterRegion).Size(FooterRows));

		layout[MainRegion].Update(Main(model));
		layout[FooterRegion].Update(Footer(model));

		return layout;
	}

	private IRenderable Main(ScreenModel model)
	{
		var content = new List<IRenderable>();

		if (model.Header is not null)
			content.Add(_panels.Banner(model.Header));

		foreach (var panel in model.Body)
			content.Add(_panels.Build(panel, _console.Profile.Width));

		return new Rows(content);
	}

	private IRenderable Footer(ScreenModel model) =>
		model.Footer is null ? new Rows() : _footer.Build(SpectreStatusBarRenderer.Fit(model.Footer, _console.Profile.Width));

	private const string MainRegion = "main";
	private const string FooterRegion = "footer";
}
