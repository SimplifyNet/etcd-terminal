using EtcdTerminal.Configuration;
using EtcdTerminal.Presentation;

namespace EtcdTerminal.App.Components;

/// <summary>
/// A view-only list page: the filter band, one table page and the pagination
/// band composed into a single frame between the banner and the footer, driven
/// by the key browser's keys — typing filters, the arrows turn pages (a mouse
/// wheel emits them too), Escape leaves. It holds no cursor and offers no
/// actions; one component owns every row of the viewport.
/// </summary>
public sealed class ListBrowser(
	ListView _view,
	ILiveFrame _live,
	IKeyReader _keys,
	IAppSettingsStore _settings)
{
	public void Show(
		IReadOnlyList<StyledText> headers,
		IReadOnlyList<IReadOnlyList<StyledText>> rows,
		string emptyText,
		string totalLabel)
	{
		var pager = new ListPager<IReadOnlyList<StyledText>>(rows, Cells);
		var query = "";
		var page = 0;
		var pageSize = _settings.Current.PageSize;

		FrameModel Frame()
		{
			var totalPages = pager.GetTotalPages(pageSize);
			var pageRows = pager.GetPage(page, pageSize);

			return _view.Frame(headers, pageRows, query, page, totalPages, pager.FilteredCount, emptyText, totalLabel);
		}

		_view.Reset();

		_live.Run(Frame(), LiveFrameEnd.Clear, updater =>
		{
			while (true)
			{
				var totalPages = pager.GetTotalPages(pageSize);
				var key = _keys.ReadKey();
				var changed = false;

				switch (key.Key)
				{
					case ConsoleKey.Escape:
						return true;
					case ConsoleKey.LeftArrow:
					case ConsoleKey.UpArrow:
						if (page > 0)
						{
							page--;
							changed = true;
						}
						break;
					case ConsoleKey.RightArrow:
					case ConsoleKey.DownArrow:
						if (page < totalPages - 1)
						{
							page++;
							changed = true;
						}
						break;
					case ConsoleKey.Backspace:
						if (query.Length > 0)
						{
							query = query[..^1];
							pager.Filter(query);
							page = 0;
							changed = true;
						}
						break;
					default:
						if (!char.IsControl(key.KeyChar))
						{
							query += key.KeyChar;
							pager.Filter(query);
							page = 0;
							changed = true;
						}
						break;
				}

				if (changed)
					updater.Update(Frame());
			}
		});
	}

	private static string Cells(IReadOnlyList<StyledText> cells) =>
		string.Join(' ', cells.Select(cell => cell.Text));
}
