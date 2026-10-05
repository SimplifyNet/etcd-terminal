using EtcdTerminal.App.Screens.Connections;
using EtcdTerminal.App.Screens.MainMenu;
using EtcdTerminal.Configuration;
using EtcdTerminal.Presentation;
using EtcdTerminal.Presentation.Localization;
using Simplify.DI;

namespace EtcdTerminal.App;

/// <summary>
/// One run of the application: the terminal session is started and stopped
/// once, and every iteration of the loop gets its own scope. A failure is
/// reported on the canvas and the loop continues.
/// </summary>
public sealed class AppRunner(ITerminalSession _terminal, IScreenCanvas _canvas, ILocalization _localization, IKeyReader _keys, IDIContainerProvider _container)
{
	private int _stopped;

	public void Start() => _terminal.Start();

	public void Stop()
	{
		if (Interlocked.Exchange(ref _stopped, 1) is not 0)
			return;

		_terminal.Stop();
		_container.Dispose();
	}

	public async Task RunAsync()
	{
		while (true)
		{
			try
			{
				using var scope = _container.BeginLifetimeScope();

				var settingsStore = scope.Resolver.Resolve<IAppSettingsStore>();

				settingsStore.Reload();

				var instanceScreen = scope.Resolver.Resolve<InstanceSelectionScreen>();
				var config = await instanceScreen.ShowAsync();

				if (config is null)
					return;

				var mainScreen = scope.Resolver.Resolve<MainScreen>();

				await mainScreen.ShowAsync();
			}
			catch (Exception ex)
			{
				_canvas.WriteException(ex);
				_canvas.Write(TextBlock.Line(new StyledText(_localization.PressAnyKeyRestart, TextRole.Muted)));

				_keys.ReadKey();
			}
		}
	}
}
