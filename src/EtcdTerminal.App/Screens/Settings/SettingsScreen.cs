using EtcdTerminal.App.Components;
using EtcdTerminal.App.Engine;
using EtcdTerminal.Presentation.Localization;

namespace EtcdTerminal.App.Screens.Settings;

public sealed class SettingsScreen(Menu _menu, PageSizeEditor _pageSize, SettingsWriter _writer, ILocalization _localization)
{
	public void Show()
	{
		while (true)
		{
			SettingsAction? action = _menu.Show<SettingsAction>(_localization.SettingsTitle,
			[
				new(SettingsAction.EditPageSize, $"{_localization.PageSizeLabel} ({_writer.Current.PageSize})"),
				new(SettingsAction.ToggleTrimInputValues, $"{_localization.TrimInputValuesLabel} ({OnOff(_writer.Current.TrimInputValues)})")
			])?.Id;

			if (action is null)
				return;

			switch (action)
			{
				case SettingsAction.EditPageSize:
					_pageSize.Edit();
					break;
				case SettingsAction.ToggleTrimInputValues:
					_writer.ToggleTrimInputValues();
					break;
			}
		}
	}

	private string OnOff(bool value) => value ? _localization.On : _localization.Off;
}
