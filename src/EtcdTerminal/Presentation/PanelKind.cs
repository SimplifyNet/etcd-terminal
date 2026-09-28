namespace EtcdTerminal.Presentation;

/// <summary>
/// Role of a panel in the composition. Selection and action panels are placed
/// and treated differently by the screen composition, not by their content;
/// <see cref="Table"/> additionally tells the mapper that every line is a
/// separate cell of a multi-column row.
/// </summary>
public enum PanelKind
{
	Default,
	Selection,
	Actions,
	Banner,
	Table
}
