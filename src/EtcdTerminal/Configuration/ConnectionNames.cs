namespace EtcdTerminal.Configuration;

/// <summary>
/// The uniqueness rule for connection names: a name is taken only when another
/// instance already uses it, so keeping your own name while editing stays
/// possible. Persistence enforces the same rule on write; this is the check the
/// editor asks before it starts a form.
/// </summary>
public static class ConnectionNames
{
	public static bool IsTaken(IReadOnlyList<EtcdConnectionConfig> instances, string name, string? exceptName) =>
		instances.Any(i => i.Name == name && i.Name != exceptName);
}
