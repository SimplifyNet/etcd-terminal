namespace EtcdTerminal.Presentation;

/// <summary>
/// Role of a panel in the composition. Selection and action panels are placed
/// and treated differently by the screen composition, not by their content;
/// <see cref="Table"/> additionally tells the mapper that every line is a
/// separate cell of a multi-column row, <see cref="Title"/> marks a section
/// heading that is separated from its surroundings by a blank line, and
/// <see cref="Block"/> marks a standalone message that starts below a blank
/// line and sits one indent further in than the menu.
/// </summary>
public enum PanelKind
{
	Default,
	Selection,
	Actions,
	Banner,
	Table,
	Title,
	Block
}
