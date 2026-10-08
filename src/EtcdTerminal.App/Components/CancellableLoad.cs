using EtcdTerminal.Presentation.Localization;

namespace EtcdTerminal.App.Components;

/// <summary>
/// Loads data behind the spinner. Escape cancels the load, warns that the
/// operation was cancelled and returns null. Failures reach the caller unchanged.
/// </summary>
public sealed class CancellableLoad(Spinner _spinner, Message _message, ILocalization _localization)
{
	public async Task<T?> RunAsync<T>(string status, Func<CancellationToken, Task<T>> load) where T : class
	{
		T? value = null;

		var loaded = await _spinner.RunAsync(status, async ct => value = await load(ct));

		if (loaded)
			return value;

		_message.ShowWarning(_localization.OperationCancelled);

		return null;
	}
}
