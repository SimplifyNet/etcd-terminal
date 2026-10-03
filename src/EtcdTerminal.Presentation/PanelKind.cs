namespace EtcdTerminal.Presentation;

/// <summary>
/// Role of a panel in the composition. <see cref="Table"/> additionally tells
/// the mapper that every line is a separate cell of a multi-column row, and
/// <see cref="Title"/> marks a section heading that is separated from its
/// surroundings by a blank line.
/// </summary>
public enum PanelKind
{
	Default,
	Banner,
	Table,
	Title
}
