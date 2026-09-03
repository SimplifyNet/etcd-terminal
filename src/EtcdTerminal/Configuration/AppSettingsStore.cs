namespace EtcdTerminal.Configuration;

public class AppSettingsStore
{
	private static IAppSettings? _current;

	public static IAppSettings Current
	{
		get
		{
			return _current ?? throw new InvalidOperationException("AppSettingsStore.Current has not been initialized.");
		}
		set
		{
			_current = value ?? throw new ArgumentNullException(nameof(value), "AppSettingsStore.Current cannot be set to null.");
		}
	}
}