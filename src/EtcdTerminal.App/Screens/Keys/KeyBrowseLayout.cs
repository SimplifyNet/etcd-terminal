using System.Globalization;
using EtcdTerminal.App.Components;
using EtcdTerminal.Keys;
using EtcdTerminal.Presentation.Localization;
using EtcdTerminal.Presentation.Terminal;
using EtcdTerminal.Presentation;

namespace EtcdTerminal.App.Screens.Keys;

/// <summary>
/// Builds the body models of the key browse frame. It holds no cursor, no
/// window width and no colors: every line is literal text with semantic roles,
/// and Infrastructure decides how much of it fits.
/// </summary>
public sealed class KeyBrowseLayout(ITerminalStyle _style, ILocalization _localization)
{
	private const string SearchPrefix = "  \U0001f50d ";
	private const string Caret = "\u2588";
	private const string PageSeparator = "  \u2022  ";

	public PanelModel Search(string searchQuery) =>
		searchQuery.Length == 0
			? Line([new StyledText(_localization.TypeToSearch, TextRole.Muted), new StyledText(Caret, TextRole.Primary)])
			: Line(
			[
				new StyledText(SearchPrefix, TextRole.Muted),
				new StyledText(ValuePreview.Sanitize(searchQuery), TextRole.Primary),
				new StyledText(Caret, TextRole.Primary)
			]);

	public PanelModel KeyList(IReadOnlyList<EtcdKeyValue> pageKeys, int selectedIndex)
	{
		if (pageKeys.Count == 0)
			return Line([new StyledText(_localization.NoKeysFound, TextRole.Muted)]);

		List<PanelLine> rows = [];

		for (var i = 0; i < pageKeys.Count; i++)
		{
			var kv = pageKeys[i];
			var role = i == selectedIndex ? TextRole.Accent : TextRole.Primary;
			var prefix = i == selectedIndex ? _style.SelectionPointer : _style.Indent;

			rows.Add(new PanelLine(
			[
				new StyledText(prefix, role),
				new StyledText(ValuePreview.Sanitize(kv.Key), role),
				new StyledText(ValuePreview.Sanitize(kv.Value), role)
			]));
		}

		return new PanelModel(rows, PanelKind.Table);
	}

	public PanelModel Pagination(int currentPage, int totalPages, int totalKeys) =>
		new(
		[
			new PanelLine(
			[
				new StyledText($"{_localization.Page} ", TextRole.Muted),
				new StyledText($"{currentPage + 1}/{totalPages}", TextRole.Primary),
				new StyledText(PageSeparator, TextRole.Muted),
				new StyledText(totalKeys.ToString(CultureInfo.InvariantCulture), TextRole.Primary),
				new StyledText($" {_localization.TotalKeys}", TextRole.Muted)
			])
		]);

	public PanelModel Detail(string label, string value, TextRole valueRole) =>
		new([new PanelLine([new StyledText(label + " ", TextRole.Default), new StyledText(value, valueRole)])]);

	public PanelModel Selected(string selectedKey) =>
		new(
		[
			new PanelLine(
			[
				new StyledText($"{_localization.Selected} ", TextRole.Muted),
				new StyledText(ValuePreview.Sanitize(selectedKey), TextRole.Accent)
			])
		], PanelKind.Selection);

	public PanelModel Actions(bool canModify)
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

		return new PanelModel([new PanelLine(hints)], PanelKind.Actions);
	}

	private static PanelModel Line(IReadOnlyList<StyledText> spans) => new([new PanelLine(spans)]);
}
