namespace EtcdTerminal.Presentation;

/// <summary>
/// Semantic meaning of a text run. Only Infrastructure translates a role into
/// a concrete color; components never name colors.
/// </summary>
public enum TextRole
{
	Default,
	Primary,
	Secondary,
	Success,
	Danger,
	Warning,
	Muted,
	Subtle,
	Accent
}
