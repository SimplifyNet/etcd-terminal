namespace EtcdTerminal.Presentation;

/// <summary>
/// Logical region a panel belongs to. Selection and action panels are placed
/// and treated differently by the screen composition, not by their content.
/// </summary>
public enum PanelKind
{
	Default,
	Selection,
	Actions,
	Banner
}
