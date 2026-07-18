using EtcdTerminal;
using Spectre.Console;

namespace EtcdTerminal.Console.Screens;

public sealed class KeySearchScreen
{
    private readonly IEtcdClient _etcdClient;

    public KeySearchScreen(IEtcdClient etcdClient)
    {
        _etcdClient = etcdClient;
    }

    public async Task ShowAsync()
    {
        var searchTerm = AnsiConsole.Ask<string>("Enter search term:");
        if (string.IsNullOrWhiteSpace(searchTerm))
            return;

        await AnsiConsole.Status()
            .StartAsync("Searching...", async ctx =>
            {
                var results = await _etcdClient.SearchKeysAsync(searchTerm);

                if (results.Count == 0)
                {
                    AnsiConsole.MarkupLine("[yellow]No keys found matching your search.[/]");
                }
                else
                {
                    var table = new Table();
                    table.AddColumn("Key");
                    table.AddColumn("Value");
                    table.AddColumn("Matched In");

                    foreach (var kv in results)
                    {
                        var matchedIn = kv.Key.Contains(searchTerm, StringComparison.OrdinalIgnoreCase)
                            ? "Key"
                            : "Value";

                        var value = kv.Value.Length > 80 ? kv.Value[..80] + "..." : kv.Value;
                        table.AddRow(
                            Markup.Escape(kv.Key),
                            Markup.Escape(value),
                            matchedIn);
                    }

                    AnsiConsole.MarkupLine($"[green]Found {results.Count} result(s):[/]");
                    AnsiConsole.Write(table);
                }
            });

        AnsiConsole.MarkupLine("[grey]Press any key to continue...[/]");
        System.Console.ReadKey(true);
    }
}
