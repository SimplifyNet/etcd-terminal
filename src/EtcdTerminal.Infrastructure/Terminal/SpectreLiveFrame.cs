using EtcdTerminal.Presentation;
using Spectre.Console;
using Spectre.Console.Rendering;

namespace EtcdTerminal.Infrastructure.Terminal;

public sealed class SpectreLiveFrame(IAnsiConsole _console, BlockRenderer _blocks) : ILiveFrame
{
	private const int FooterRows = 3;

	public T Run<T>(FrameModel initial, LiveFrameEnd end, Func<ILiveFrameUpdater, T> interaction)
	{
		try
		{
			return _console.Live(Render(initial))
				.AutoClear(end is LiveFrameEnd.Clear)
				.Overflow(VerticalOverflow.Crop)
				.Start(ctx =>
				{
					ctx.Refresh();

					return interaction(new Updater(ctx, this));
				});
		}
		finally
		{
			_console.Cursor.Show(false);
		}
	}

	private IRenderable Render(FrameModel model) =>
		new ClampedRows(new Rows(model.Body.Select(_blocks.Render)));

	private sealed class Updater(LiveDisplayContext _ctx, SpectreLiveFrame _owner) : ILiveFrameUpdater
	{
		public void Update(FrameModel model)
		{
			_ctx.UpdateTarget(_owner.Render(model));
			_ctx.Refresh();
		}
	}

	/// Crops to the viewport above the footer; Live itself crops only at full
	/// console height. The height is read when rendering, so a redraw after
	/// the window was resized fits the new viewport.
	private sealed class ClampedRows(IRenderable _inner) : Renderable
	{
		protected override Measurement Measure(RenderOptions options, int maxWidth) => _inner.Measure(options, maxWidth);

		protected override IEnumerable<Segment> Render(RenderOptions options, int maxWidth)
		{
			var lines = Segment.SplitLines(_inner.Render(options, maxWidth));
			var result = new List<Segment>();

			foreach (var line in lines.Take(Math.Max(1, options.ConsoleSize.Height - FooterRows)))
			{
				result.AddRange(line);
				result.Add(Segment.LineBreak);
			}

			return result;
		}
	}
}
