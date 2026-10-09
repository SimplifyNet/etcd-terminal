using EtcdTerminal.App.Components;
using EtcdTerminal.Keys;
using EtcdTerminal.Presentation.Localization;
using EtcdTerminal.Presentation;

namespace EtcdTerminal.App.Screens.Keys;

/// <summary>
/// Builds the body models of the key browse frame. It holds no cursor, no
/// window width and no colors: every line is literal text with semantic roles,
/// and Infrastructure decides how much of it fits. The filter and pagination
/// bands are shared with every other browsable list and live in
/// <see cref="BrowseLayout"/>.
/// </summary>
public sealed class KeyBrowseLayout(ILocalizationCatalog _localizations)
{
	// The pointer of the selected row hangs on the margin column and the key
	// after it starts on the text column, the menu's layout; the other rows
	// fill the margin with spaces so every key stays on the same column.
	private const string Pointer = "\u276f ";
	private const string Marker = "  ";

	public Block KeyList(IReadOnlyList<EtcdKeyValue> pageKeys, int selectedIndex)
	{
		if (pageKeys.Count == 0)
			return TextBlock.Line(new StyledText(_localizations.Current.NoKeysFound, TextRole.Muted));

		List<IReadOnlyList<StyledText>> rows = [];

		for (var i = 0; i < pageKeys.Count; i++)
		{
			var kv = pageKeys[i];
			var selected = i == selectedIndex;
			var role = selected ? TextRole.Accent : TextRole.Primary;

			rows.Add(
			[
				new StyledText(selected ? Pointer : Marker, role),
				new StyledText(DisplayText.Sanitize(kv.Key), role),
				new StyledText(DisplayText.Sanitize(kv.Value), role)
			]);
		}

		return new TableBlock([], rows) { Pointer = true };
	}

	public Block Detail(string label, string value, TextRole valueRole) =>
		TextBlock.Line(new StyledText(label + " ", TextRole.Default), new StyledText(value, valueRole));

	public Block ActionPanel(string selectedKey, bool canModify)
	{
		List<StyledText> hints = [];

		if (canModify)
		{
			hints.Add(new StyledText("E", TextRole.Primary));
			hints.Add(new StyledText($" {_localizations.Current.Edit}", TextRole.Muted));
			hints.Add(new StyledText("   ", TextRole.Muted));
			hints.Add(new StyledText("D", TextRole.Primary));
			hints.Add(new StyledText($" {_localizations.Current.Delete}", TextRole.Muted));
			hints.Add(new StyledText("   ", TextRole.Muted));
		}

		hints.Add(new StyledText("Esc", TextRole.Primary));
		hints.Add(new StyledText($" {_localizations.Current.Cancel}", TextRole.Muted));

		return new ActionPanelBlock(
			[new StyledText($"{_localizations.Current.Selected} ", TextRole.Muted), new StyledText(DisplayText.Sanitize(selectedKey), TextRole.Accent)],
			hints);
	}
}
