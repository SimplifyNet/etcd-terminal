using Spectre.Console;

namespace EtcdTerminal.App.Components;

public static class Header
{
	public static void Render()
	{
		AnsiConsole.Write(new FigletText("etcd-terminal").Color(Color.OrangeRed1).Centered());
	}
}
