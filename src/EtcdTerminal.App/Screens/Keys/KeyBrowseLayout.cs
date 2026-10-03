using System.Globalization;
using EtcdTerminal.App.Components;
using EtcdTerminal.Keys;
using EtcdTerminal.Presentation.Localization;
using EtcdTerminal.Presentation;

namespace EtcdTerminal.App.Screens.Keys;

/// <summary>
/// Builds the body models of the key browse frame. It holds no cursor, no
/// window width and no colors: every line is literal text with semantic roles,
/// and Infrastructure decides how much of it fits.
/// </summary>
public sealed class KeyBrowseLayout(ILocalization _localization)
{
	private const string SearchPrefix = "  \U0001f50d ";
	private const string Caret = "\u2588";
	private const string PageSeparator = "  \u2022  ";

	public Block Search(string searchQuery) =>
		searchQuery.Length == 0
			? TextBlock.Line(new StyledText(_localization.TypeToSearch, TextRole.Muted), new StyledText(Caret, TextRole.Primary))
			: TextBlock.Line(
				new StyledText(SearchPrefix, TextRole.Muted),
				new StyledText(DisplayText.Sanitize(searchQuery), TextRole.Primary),
				new StyledText(Caret, TextRole.Primary));

	public Block KeyList(IReadOnlyList<EtcdKeyValue> pageKeys, int selectedIndex)
	{
		if (pageKeys.Count == 0)
			return TextBlock.Line(new StyledText(_localization.NoKeysFound, TextRole.Muted));

		List<IReadOnlyList<StyledText>> rows = [];

		for (var i = 0; i < pageKeys.Count; i++)
		{
			var kv = pageKeys[i];
			var role = i == selectedIndex ? TextRole.Accent : TextRole.Primary;

			rows.Add(
			[
				new StyledText(DisplayText.Sanitize(kv.Key), role),
				new StyledText(DisplayText.Sanitize(kv.Value), role)
			]);
		}

		return new TableBlock([], rows);
	}

	public Block Pagination(int currentPage, int totalPages, int totalKeys) =>
		TextBlock.Line(
			new StyledText($"{_localization.Page} ", TextRole.Muted),
			new StyledText($"{currentPage + 1}/{totalPages}", TextRole.Primary),
			new StyledText(PageSeparator, TextRole.Muted),
			new StyledText(totalKeys.ToString(CultureInfo.InvariantCulture), TextRole.Primary),
			new StyledText($" {_localization.TotalKeys}", TextRole.Muted));

	public Block Detail(string label, string value, TextRole valueRole) =>
		TextBlock.Line(new StyledText(label + " ", TextRole.Default), new StyledText(value, valueRole));

	public Block Selected(string selectedKey) =>
		TextBlock.Line(
			new StyledText($"{_localization.Selected} ", TextRole.Muted),
			new StyledText(DisplayText.Sanitize(selectedKey), TextRole.Accent));

	public Block Actions(bool canModify)
	{
		List<StyledText> hints = [];

		if (canModify)
		{
			hints.Add(new StyledText("E", TextRole.Primary));
			hints.Add(new StyledText($" {_localization.Edit}", TextRole.Muted));
			hints.Add(new StyledText("   ", TextRole.Muted));
			hints.Add(new StyledText("D", TextRole.Primary));
			hints.Add(new StyledText($" {_localization.Delete}", TextRole.Muted));
			hints.Add(new StyledText("   ", TextRole.Muted));
		}

		hints.Add(new StyledText("Esc", TextRole.Primary));
		hints.Add(new StyledText($" {_localization.Cancel}", TextRole.Muted));

		return TextBlock.Line([.. hints]);
	}
}
