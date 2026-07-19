using EtcdTerminal;
using EtcdTerminal.App.Engine;
using EtcdTerminal.App.Modules;
using EtcdTerminal.Models;
using Spectre.Console;

namespace EtcdTerminal.App.Screens;

public sealed class PermissionViewScreen(IEtcdClient _etcdClient)
{
	public async Task ShowAsync(EtcdConnectionConfig config)
	{
		AnsiConsole.Clear();
		StatusBar.Render(config);

		await AnsiConsole.Status()
			.StartAsync("Loading permissions...", async ctx =>
			{
				var users = await _etcdClient.GetUsersAsync();
				var roles = await _etcdClient.GetRolesAsync();

				if (users.Count == 0 && roles.Count == 0)
				{
					AnsiConsole.MarkupLine("[yellow]No users or roles found.[/]");

					return;
				}

				foreach (var user in users)
				{
					var table = new Table();
					table.Title = new TableTitle($"[bold]User: {user.Username}[/]");
					table.AddColumn("Role");
					table.AddColumn("Permissions");

					if (user.Roles.Count == 0)
						table.AddRow("[grey]no roles[/]", "[grey]-[/]");
					else
					{
						foreach (var roleName in user.Roles)
						{
							var role = roles.FirstOrDefault(r => r.Name == roleName);
							var permissions = role is not null && role.Permissions.Count > 0
								? string.Join("\n", role.Permissions.Select(p => $"{p.Type}: {p.KeyPrefix}"))
								: "[grey]no permissions[/]";

							table.AddRow(Markup.Escape(roleName), permissions);
						}
					}

					AnsiConsole.Write(table);
					AnsiConsole.WriteLine();
				}
			});

		AnsiConsole.MarkupLine(Prompt.PressAnyKeyMarkup);
		Console.ReadKey(true);
	}
}
