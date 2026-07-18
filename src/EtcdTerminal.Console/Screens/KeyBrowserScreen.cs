using EtcdTerminal.Console.Helpers;
using EtcdTerminal.Models;
using Spectre.Console;

namespace EtcdTerminal.Console.Screens;

public sealed class KeyBrowserScreen(IEtcdClient _etcdClient)
{
	public async Task ShowAsync(EtcdConnectionConfig config)
	{
		StatusBar.Render(config);

		var prefix = AnsiConsole.Ask<string>("Enter key prefix (default: [green]/[/]):", "/");

		if (string.IsNullOrWhiteSpace(prefix))
			prefix = "/";

		var keys = await _etcdClient.GetKeysByPrefixAsync(prefix);

		if (keys.Count == 0)
			AnsiConsole.MarkupLine("[yellow]No keys found.[/]");
		else
		{
			var table = new Table();

			table.AddColumn("Key");
			table.AddColumn("Value");
			table.AddColumn("Version");
			table.AddColumn("Mod Revision");

			foreach (var kv in keys)
			{
				var value = kv.Value.Length > 80 ? kv.Value[..80] + "..." : kv.Value;

				table.AddRow(
					Markup.Escape(kv.Key),
					Markup.Escape(value),
					kv.Version.ToString(),
					kv.ModRevision.ToString());
			}

			AnsiConsole.Write(table);
		}

		AnsiConsole.MarkupLine("[grey]Press any key to continue...[/]");
		System.Console.ReadKey(true);
	}
}
