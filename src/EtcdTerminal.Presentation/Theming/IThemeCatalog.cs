namespace EtcdTerminal.Presentation.Theming;

public interface IThemeCatalog
{
	IReadOnlyList<ITheme> Themes { get; }
	ITheme Current { get; }

	/// <summary>
	/// Raised after <see cref="Current"/> changed, so the renderers that
	/// painted the console with the old palette can repaint it.
	/// </summary>
	event Action? Changed;

	/// <summary>
	/// Makes the theme with the given id current. An unknown id falls back to
	/// the first theme, so a stale settings file never breaks the start.
	/// </summary>
	void Set(string? themeId);
}