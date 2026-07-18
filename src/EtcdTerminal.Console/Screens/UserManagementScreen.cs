using EtcdTerminal;
using Spectre.Console;

namespace EtcdTerminal.Console.Screens;

public sealed class UserManagementScreen
{
    private readonly IEtcdClient _etcdClient;

    public UserManagementScreen(IEtcdClient etcdClient)
    {
        _etcdClient = etcdClient;
    }

    public async Task ShowAsync()
    {
        var running = true;
        while (running)
        {
            AnsiConsole.Clear();
            var choice = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("User Management")
                    .PageSize(10)
                    .AddChoices(
                        "List Users",
                        "Create User",
                        "Delete User",
                        "Change Password",
                        "Assign Role to User",
                        "Remove Role from User",
                        "Back"));

            switch (choice)
            {
                case "List Users":
                    await ListUsersAsync();
                    break;
                case "Create User":
                    await CreateUserAsync();
                    break;
                case "Delete User":
                    await DeleteUserAsync();
                    break;
                case "Change Password":
                    await ChangePasswordAsync();
                    break;
                case "Assign Role to User":
                    await AssignRoleAsync();
                    break;
                case "Remove Role from User":
                    await RevokeRoleAsync();
                    break;
                case "Back":
                    running = false;
                    break;
            }
        }
    }

    private async Task ListUsersAsync()
    {
        var users = await _etcdClient.GetUsersAsync();
        if (users.Count == 0)
        {
            AnsiConsole.MarkupLine("[yellow]No users found.[/]");
        }
        else
        {
            var table = new Table();
            table.AddColumn("Username");
            table.AddColumn("Roles");

            foreach (var user in users)
            {
                var roles = user.Roles.Count > 0
                    ? string.Join(", ", user.Roles)
                    : "[grey]none[/]";
                table.AddRow(Markup.Escape(user.Username), roles);
            }

            AnsiConsole.Write(table);
        }

        AnsiConsole.MarkupLine("[grey]Press any key to continue...[/]");
        System.Console.ReadKey(true);
    }

    private async Task CreateUserAsync()
    {
        var username = AnsiConsole.Ask<string>("Enter username:");
        var password = AnsiConsole.Prompt(
            new TextPrompt<string>("Enter password:")
                .Secret());

        var result = await _etcdClient.CreateUserAsync(username, password);
        if (result)
            AnsiConsole.MarkupLine("[green]User created successfully![/]");
        else
            AnsiConsole.MarkupLine("[red]Failed to create user.[/]");

        AnsiConsole.MarkupLine("[grey]Press any key to continue...[/]");
        System.Console.ReadKey(true);
    }

    private async Task DeleteUserAsync()
    {
        var username = AnsiConsole.Ask<string>("Enter username to delete:");

        if (!AnsiConsole.Confirm($"Are you sure you want to delete user [red]{username}[/]?"))
            return;

        var result = await _etcdClient.DeleteUserAsync(username);
        if (result)
            AnsiConsole.MarkupLine("[green]User deleted successfully![/]");
        else
            AnsiConsole.MarkupLine("[red]Failed to delete user.[/]");

        AnsiConsole.MarkupLine("[grey]Press any key to continue...[/]");
        System.Console.ReadKey(true);
    }

    private async Task ChangePasswordAsync()
    {
        var username = AnsiConsole.Ask<string>("Enter username:");
        var newPassword = AnsiConsole.Prompt(
            new TextPrompt<string>("Enter new password:")
                .Secret());

        var result = await _etcdClient.ChangeUserPasswordAsync(username, newPassword);
        if (result)
            AnsiConsole.MarkupLine("[green]Password changed successfully![/]");
        else
            AnsiConsole.MarkupLine("[red]Failed to change password.[/]");

        AnsiConsole.MarkupLine("[grey]Press any key to continue...[/]");
        System.Console.ReadKey(true);
    }

    private async Task AssignRoleAsync()
    {
        var username = AnsiConsole.Ask<string>("Enter username:");
        var roleName = AnsiConsole.Ask<string>("Enter role name:");

        try
        {
            await _etcdClient.GrantRoleToUserAsync(username, roleName);
            AnsiConsole.MarkupLine("[green]Role assigned successfully![/]");
        }
        catch
        {
            AnsiConsole.MarkupLine("[red]Failed to assign role.[/]");
        }

        AnsiConsole.MarkupLine("[grey]Press any key to continue...[/]");
        System.Console.ReadKey(true);
    }

    private async Task RevokeRoleAsync()
    {
        var username = AnsiConsole.Ask<string>("Enter username:");
        var roleName = AnsiConsole.Ask<string>("Enter role name to remove:");

        try
        {
            await _etcdClient.RevokeRoleFromUserAsync(username, roleName);
            AnsiConsole.MarkupLine("[green]Role removed successfully![/]");
        }
        catch
        {
            AnsiConsole.MarkupLine("[red]Failed to remove role.[/]");
        }

        AnsiConsole.MarkupLine("[grey]Press any key to continue...[/]");
        System.Console.ReadKey(true);
    }
}
