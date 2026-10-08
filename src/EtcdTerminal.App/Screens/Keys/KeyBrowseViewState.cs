using EtcdTerminal.Keys;

namespace EtcdTerminal.App.Screens.Keys;

public sealed record KeyBrowseViewState(string SearchQuery, int CurrentPage, int SelectedIndex, EtcdKeyValue? SelectedKey, bool ShowActions, bool CanModify);
