using EtcdTerminal.App.Theming.Themes;
using EtcdTerminal.Presentation.Theming;

namespace EtcdTerminal.App.Theming;

public sealed class ThemeCatalog : IThemeCatalog
{
	private readonly IReadOnlyList<ITheme> _themes =
	[
		new ReddyTheme(),
		new EtcdDarkTheme(),
		new EtcdBlueTheme(),
		new UbuntuTheme(),
		new ManjaroTheme(),
		new PopTheme(),
		new SolarizedDarkTheme(),
		new DarkVioletTheme(),
		new MatrixTheme(),
		new LightReddyTheme(),
		new VsCodeLightTheme(),
		new SolarizedLightTheme()
	];

	private ITheme _current;

	public ThemeCatalog() => _current = _themes[0];

	public IReadOnlyList<ITheme> Themes => _themes;
	public ITheme Current => _current;

	public event Action? Changed;

	public void Set(string? themeId)
	{
		var theme = _themes.FirstOrDefault(item => item.Id == themeId) ?? _themes[0];

		if (ReferenceEquals(_current, theme))
			return;

		_current = theme;

		Changed?.Invoke();
	}
}
