using EtcdTerminal;
using EtcdTerminal.App.Engine;
using EtcdTerminal.App.Components;
using EtcdTerminal.Models;
using Spectre.Console;

namespace EtcdTerminal.App.Screens;

public sealed class PermissionViewScreen(IEtcdClient _etcdClient)
{
	private const string LoadingPermissions = "Loading permissions...";
	private const string NoUsersOrRoles = "[yellow]No users or roles found.[/]";

	public async Task ShowAsync(EtcdConnectionConfig config)
	{
		AnsiConsole.Clear();
		Header.Render();
		var savedTop = Console.CursorTop;
		StatusBar.Render(config);
		Console.CursorTop = savedTop;
		Console.CursorLeft = 0;

		await AnsiConsole.Status()
			.StartAsync(LoadingPermissions, async ctx =>
			{
				var users = await _etcdClient.GetUsersAsync();
				var roles = await _etcdClient.GetRolesAsync();

				if (users.Count == 0 && roles.Count == 0)
				{
					AnsiConsole.MarkupLine(NoUsersOrRoles);

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
