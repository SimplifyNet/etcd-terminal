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
		model.Footer is null ? new Rows() : _footer.Build(model.Footer);

	private const string MainRegion = "main";
	private const string FooterRegion = "footer";
}
