namespace EtcdTerminal.Localization;

public class LocalizationStore
{
	private static ILocalization? _current;

	public static ILocalization Current
	{
		get
		{
			return _current ?? throw new InvalidOperationException("LocalizationStore.Current has not been initialized.");
		}
		set
		{
			_current = value ?? throw new ArgumentNullException(nameof(value), "LocalizationStore.Current cannot be set to null.");
		}
	}
}