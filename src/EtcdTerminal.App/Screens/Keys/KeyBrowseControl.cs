using EtcdTerminal.App.Components;
using EtcdTerminal.Keys;
using EtcdTerminal.Presentation;
using EtcdTerminal.Session;
using EtcdTerminal.Terminal;

namespace EtcdTerminal.App.Screens.Keys;

public sealed class KeyBrowseControl(
	ITerminalInput _input,
	KeyBrowseLayout _layout,
	IScreenHost _host,
	Header _header,
	StatusBar _statusBar,
	IConnectionSession _session)
{
	private bool _frameOpen;

	public string SearchQuery { get; private set; } = "";
	public int CurrentPage { get; private set; }
	public int SelectedIndex { get; private set; }
	private bool ShowActions { get; set; }
	public EtcdKeyValue? SelectedKey { get; private set; }

	/// <summary>
	/// Edit and delete are offered only when the account may write the selected key.
	/// </summary>
	private bool CanModifySelectedKey => SelectedKey is not null && _session.Capabilities.CanWriteKey(SelectedKey.Key);

	/// <summary>
	/// Paints the browse frame, replacing the previous one in place. The first
	/// call takes the console; later calls update the same frame.
	/// </summary>
	public void Render(IReadOnlyList<EtcdKeyValue> pageKeys, int totalPages, int totalKeys)
	{
		var model = Frame([.. Body(pageKeys, totalPages, totalKeys)]);

		if (_frameOpen)
			_host.Update(model);
		else
		{
			_host.Begin(model);
			_frameOpen = true;
		}
	}

	/// <summary>
	/// Shows a short frame of its own, for example the details written before a
	/// prompt. The console is handed back so the prompt can draw underneath.
	/// </summary>
	public void ShowDetails(params IReadOnlyList<PanelModel> panels)
	{
		Release();
		_host.Begin(Frame(panels));
		_host.End();
	}

	/// <summary>
	/// Hands the console back. Safe to call when nothing is being shown.
	/// </summary>
	public void Release()
	{
		if (!_frameOpen)
			return;

		_host.End();
		_frameOpen = false;
	}

	public KeyBrowseCommand ReadCommand(IReadOnlyList<EtcdKeyValue> pageKeys, int totalPages)
	{
		var key = _input.ReadKey();

		if (ShowActions)
		{
			var canModify = CanModifySelectedKey;

			switch (key.Key)
			{
				case ConsoleKey.Escape:
					ShowActions = false;
					SelectedKey = null;
					break;
				case ConsoleKey.E when canModify:
					var editKey = SelectedKey;

					ShowActions = false;
					SelectedKey = null;
					return new KeyBrowseCommand(KeyBrowseAction.Edit, editKey);
				case ConsoleKey.D when canModify:
					var deleteKey = SelectedKey;

					ShowActions = false;
					SelectedKey = null;
					return new KeyBrowseCommand(KeyBrowseAction.Delete, deleteKey);
			}

			return KeyBrowseCommand.None;
		}

		switch (key.Key)
		{
			case ConsoleKey.UpArrow:
				if (SelectedIndex > 0)
					SelectedIndex--;
				break;
			case ConsoleKey.DownArrow:
				if (SelectedIndex < pageKeys.Count - 1)
					SelectedIndex++;
				break;
			case ConsoleKey.LeftArrow:
				if (CurrentPage > 0)
				{
					CurrentPage--;
					SelectedIndex = 0;
				}
				break;
			case ConsoleKey.RightArrow:
				if (CurrentPage < totalPages - 1)
				{
					CurrentPage++;
					SelectedIndex = 0;
				}
				break;
			case ConsoleKey.Enter:
				if (pageKeys.Count > 0)
				{
					SelectedKey = pageKeys[Math.Min(SelectedIndex, pageKeys.Count - 1)];
					ShowActions = true;
				}
				break;
			case ConsoleKey.Backspace:
				if (SearchQuery.Length > 0)
				{
					SearchQuery = SearchQuery[..^1];
					return new KeyBrowseCommand(KeyBrowseAction.SearchChanged, null);
				}
				break;
			case ConsoleKey.Escape:
				return new KeyBrowseCommand(KeyBrowseAction.Exit, null);
			default:
				if (!char.IsControl(key.KeyChar))
				{
					SearchQuery += key.KeyChar;
					return new KeyBrowseCommand(KeyBrowseAction.SearchChanged, null);
				}
				break;
		}

		return KeyBrowseCommand.None;
	}

	public void ResetNavigation()
	{
		CurrentPage = 0;
		SelectedIndex = 0;
	}

	public void ClearSearch()
	{
		SearchQuery = "";
		ShowActions = false;
		SelectedKey = null;
	}

	public void ClampPage(int totalPages)
	{
		CurrentPage = Math.Clamp(CurrentPage, 0, Math.Max(0, totalPages - 1));
		SelectedIndex = 0;
	}

	private IEnumerable<PanelModel> Body(IReadOnlyList<EtcdKeyValue> pageKeys, int totalPages, int totalKeys)
	{
		yield return _layout.Search(SearchQuery);
		yield return _layout.KeyList(pageKeys, SelectedIndex);
		yield return _layout.Pagination(CurrentPage, totalPages, totalKeys);

		if (!ShowActions || SelectedKey is null)
			yield break;

		yield return _layout.Selected(SelectedKey.Key);
		yield return _layout.Actions(CanModifySelectedKey);
	}

	private ScreenModel Frame(IReadOnlyList<PanelModel> body) =>
		new()
		{
			Header = _header.BuildModel(),
			Body = body,
			Footer = _statusBar.BuildModel()
		};
}
