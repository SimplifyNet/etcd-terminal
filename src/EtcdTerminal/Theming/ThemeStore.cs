namespace EtcdTerminal.Theming;

public class ThemeStore
{
	private static ITheme? _current;

	public static ITheme Current
	{
		get
		{
			return _current ?? throw new InvalidOperationException("ThemeStore.Current has not been initialized.");
		}
		set
		{
			_current = value ?? throw new ArgumentNullException(nameof(value), "ThemeStore.Current cannot be set to null.");
		}
	}
}