using EtcdTerminal.Presentation;
using EtcdTerminal.Presentation.Localization;
using Spectre.Console;
using Spectre.Console.Rendering;

namespace EtcdTerminal.Infrastructure.Terminal;

/// <summary>
/// The selection menu. The library prompt cannot draw it: its pointer is a
/// hardcoded ">" and its rows cannot be indented (documented in 0.57.2), so
/// the rows are composed here. The region, the cursor and the erasing of the
/// menu when it closes stay with the library's live renderer, which is what
/// the prompt used before, so the rest of the console is untouched. The rows are
/// composed when the region is rendered, so a redraw of the screen after the
/// window was resized shows them for the new height.
/// </summary>
public sealed class SpectreSelectionPrompt(IAnsiConsole _console, RoleStyleMapper _styles, ILocalization _localization) : ISelectionPrompt
{
	/// The menu, its title and its hint have to leave the rows of the screen
	/// to the header and the footer of the screen that owns the prompt.
	private const int ReservedRows = 14;

	public Choice<TId>? Select<TId>(ChoiceList<TId> list)
	{
		if (list.Items.Count is 0)
			return null;

		var index = 0;

		try
		{
			return _console.Live(new Target<TId>(this, list, () => index))
				.AutoClear(true)
				.Overflow(VerticalOverflow.Crop)
				.Start(ctx =>
				{
					ctx.Refresh();

					while (true)
					{
						var key = _console.Input.ReadKey(true);

						if (key is null)
							continue;

						if (key.Value.Key is ConsoleKey.Escape)
							return null;

						if (key.Value.Key is ConsoleKey.Enter or ConsoleKey.Packet or ConsoleKey.Spacebar)
							return list.Items[index];

						var next = Step(key.Value.Key, index, list.Items.Count, PageSize(_console.Profile.Height));

						if (next != index)
						{
							index = next;

							ctx.Refresh();
						}
					}
				});
		}
		finally
		{
			// The live renderer reveals the cursor when it closes its region;
			// the session keeps it hidden so nothing blinks after the menu.
			_console.Cursor.Show(false);
		}
	}

	/// <summary>
	/// The title, the visible window of the list and the hint that there is
	/// more below it. The current row carries the pointer on the second column
	/// and every row its text on the fourth, the margin the terminal drew
	/// before the migration.
	/// </summary>
	private IRenderable Render<TId>(ChoiceList<TId> list, int index, int pageSize)
	{
		var rows = new List<IRenderable>();

		if (!string.IsNullOrEmpty(list.Title))
		{
			rows.Add(Row(ContentIndent.Text + list.Title, Style.Plain));
			rows.Add(_styles.Build([]));
		}

		var (skip, take) = Window(list.Items.Count, index, pageSize);

		for (var position = skip; position < skip + take; position++)
		{
			var current = position == index;
			var prefix = current ? ContentIndent.SelectionPointer : ContentIndent.Text;

			rows.Add(Row(prefix + list.Items[position].Label, current ? _styles.Resolve(TextRole.Accent) : Style.Plain));
		}

		if (list.Items.Count > pageSize)
		{
			rows.Add(_styles.Build([]));
			rows.Add(Row(ContentIndent.Text + _localization.MoreChoices, Style.Plain));
		}

		return new Rows(rows);
	}

	/// <summary>
	/// The window of rows around the current one, the pointer kept in the
	/// middle until the ends of the list pull it back.
	/// </summary>
	private static (int Skip, int Take) Window(int count, int index, int pageSize)
	{
		if (count <= pageSize)
			return (0, count);

		var skip = Math.Max(0, Math.Min(index - pageSize / 2, count - pageSize));

		return (skip, Math.Min(pageSize, count - skip));
	}

	/// <summary>
	/// The keys of the prompt it replaced: the arrows and their vi aliases,
	/// the ends of the list and a page at a time, wrapping at both ends.
	/// </summary>
	private static int Step(ConsoleKey key, int index, int count, int pageSize)
	{
		var next = key switch
		{
			ConsoleKey.UpArrow or ConsoleKey.K => index - 1,
			ConsoleKey.DownArrow or ConsoleKey.J => index + 1,
			ConsoleKey.Home => 0,
			ConsoleKey.End => count - 1,
			ConsoleKey.PageUp => index - pageSize,
			ConsoleKey.PageDown => index + pageSize,
			_ => index
		};

		return (count + (next % count)) % count;
	}

	private static int PageSize(int height) => Math.Max(3, height - ReservedRows);

	private static Paragraph Row(string text, Style style)
	{
		var paragraph = new Paragraph();

		paragraph.Append(text, style);
		paragraph.Overflow = Overflow.Ellipsis;

		return paragraph;
	}

	/// The live region's target: composes the rows for the current index and
	/// the height at the time of rendering, so every refresh — including the
	/// one a screen redraw triggers — fits the current window.
	private sealed class Target<TId>(SpectreSelectionPrompt _owner, ChoiceList<TId> _list, Func<int> _index) : Renderable
	{
		protected override IEnumerable<Segment> Render(RenderOptions options, int maxWidth) =>
			_owner.Render(_list, _index(), PageSize(options.ConsoleSize.Height)).Render(options, maxWidth);
	}
}
