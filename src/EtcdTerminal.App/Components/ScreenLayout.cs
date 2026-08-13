using EtcdTerminal.Configuration;
using Spectre.Console;

namespace EtcdTerminal.App.Components;

public static class ScreenLayout
{
	public static void RenderHeader(EtcdConnectionConfig? config)
	{
		AnsiConsole.Clear();
		Header.Render();

		var savedTop = Console.CursorTop;

		StatusBar.Render(config);
		Console.CursorTop = savedTop;
		Console.CursorLeft = 0;
	}
}
