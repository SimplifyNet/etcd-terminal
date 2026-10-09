using EtcdTerminal.App.Screens.Connections;
using EtcdTerminal.App.Screens.MainMenu;
using EtcdTerminal.App.Screens.Settings;
using EtcdTerminal.App.Setup;
using EtcdTerminal.Presentation;
using EtcdTerminal.Presentation.Localization;
using Simplify.DI;

namespace EtcdTerminal.App;

/// <summary>
/// One run of the application: the terminal session is started and stopped
/// once, and every iteration of the loop gets its own scope. A failure is
/// reported on the canvas and the loop continues.
/// </summary>
public sealed class AppRunner(ITerminalSession _terminal, IScreenCanvas _canvas, ILocalizationCatalog _languages, IKeyReader _keys)
{
	private int _stopped;

	public void Start()
	{
		_terminal.OnResize(_canvas.Redraw);
		_terminal.Start();
	}

	public void Stop()
	{
		if (Interlocked.Exchange(ref _stopped, 1) is not 0)
			return;

		_terminal.Stop();
	}

	public async Task RunAsync()
	{
		while (true)
		{
			try
			{
				if (!await RunIterationAsync())
					return;
			}
			catch (Exception ex)
			{
				_canvas.WriteException(ex);
				_canvas.Write(TextBlock.Line(new StyledText(_languages.Current.PressAnyKeyRestart, TextRole.Muted)));

				while (ScrollKeys.Step(_keys.ReadKey()) is not null)
				{
					// A wheel that arrives as arrow keys must not restart the app.
				}
			}
		}
	}

	private async Task<bool> RunIterationAsync()
	{
		using var scope = DIContainer.Current.BeginLifetimeScope();

		scope.Resolver.Resolve<PreferencesLoader>().Load();
		scope.Resolver.Resolve<LanguagePreferenceEditor>().ShowIfMissing();
		scope.Resolver.Resolve<ThemePreferenceEditor>().ShowIfMissing();

		var language = _languages.Current;
		var instanceScreen = scope.Resolver.Resolve<InstanceSelectionScreen>();
		var config = await instanceScreen.ShowAsync();

		// The screens of this scope were built with the old text; a new
		// iteration rebuilds them in the chosen language.
		if (!ReferenceEquals(_languages.Current, language))
			return true;

		if (config is null)
			return false;

		await ShowMainScreenAsync(scope);

		return true;
	}

	private static async Task ShowMainScreenAsync(ILifetimeScope scope)
	{
		var mainScreen = scope.Resolver.Resolve<MainScreen>();

		await mainScreen.ShowAsync();
	}
}
