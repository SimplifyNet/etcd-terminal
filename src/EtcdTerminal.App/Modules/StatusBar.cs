namespace EtcdTerminal.App.Modules;

public static class StatusBar
{
	public static void Render()
	{
		var bgSeq = "\x1b[48;2;27;28;30m";
		var fgSeq = "\x1b[38;2;210;210;210m";
		var resetSeq = "\x1b[0m";
		var fill = new string(' ', Console.WindowWidth);
		var hintText = "  (\u2191/\u2193 navigate, Enter confirm, Esc back)  ";
		var hintLine = (hintText + fill)[..Console.WindowWidth];

		Console.CursorTop = Console.WindowHeight - 3;
		Console.CursorLeft = 0;
		Console.Write(bgSeq + fill + resetSeq);

		Console.CursorTop = Console.WindowHeight - 2;
		Console.CursorLeft = 0;
		Console.Write(bgSeq + fgSeq + hintLine + resetSeq);

		Console.CursorTop = Console.WindowHeight - 1;
		Console.CursorLeft = 0;
		Console.Write(bgSeq + fill + resetSeq);
	}
}
