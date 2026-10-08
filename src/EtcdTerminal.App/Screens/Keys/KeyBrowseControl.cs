using EtcdTerminal.Keys;
using EtcdTerminal.Presentation;
using EtcdTerminal.Session;

namespace EtcdTerminal.App.Screens.Keys;

public sealed class KeyBrowseControl(
	IKeyReader _keys,
	IConnectionSession _session,
	KeyBrowseView _view)
{
	public string SearchQuery { get; private set; } = "";
	public int CurrentPage { get; private set; }
	public int SelectedIndex { get; private set; }
	public EtcdKeyValue? SelectedKey { get; private set; }

	private bool ShowActions { get; set; }

	/// Edit and delete are offered only when the account may write the selected key.
	private bool CanModifySelectedKey => SelectedKey is not null && _session.Capabilities.CanWriteKey(SelectedKey.Key);

	public FrameModel Frame(IReadOnlyList<EtcdKeyValue> pageKeys, int totalPages, int totalKeys) =>
		_view.Frame(new(SearchQuery, CurrentPage, SelectedIndex, SelectedKey, ShowActions, CanModifySelectedKey), pageKeys, totalPages, totalKeys);

	public KeyBrowseCommand ReadCommand(IReadOnlyList<EtcdKeyValue> pageKeys, int totalPages)
	{
		var key = _keys.ReadKey();

		return ShowActions
			? ReadActionCommand(key)
			: ReadNavigationCommand(key, pageKeys, totalPages);
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

	private KeyBrowseCommand ReadActionCommand(ConsoleKeyInfo key)
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

	private KeyBrowseCommand ReadNavigationCommand(ConsoleKeyInfo key, IReadOnlyList<EtcdKeyValue> pageKeys, int totalPages)
	{
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
}
