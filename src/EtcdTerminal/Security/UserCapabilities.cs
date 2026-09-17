using EtcdTerminal.Permissions;

namespace EtcdTerminal.Security;

/// <summary>
/// What the currently connected account is actually allowed to do on the etcd cluster.
/// </summary>
public sealed record UserCapabilities
{
	/// <summary>
	/// Capabilities used when authentication is disabled or the account owns the root role.
	/// </summary>
	public static readonly UserCapabilities Unrestricted = new() { IsRoot = true };

	/// <summary>
	/// True when the account has unrestricted access (root role or authentication disabled).
	/// </summary>
	public bool IsRoot { get; init; }

	/// <summary>
	/// Effective key permissions collected from all roles granted to the account.
	/// </summary>
	public IReadOnlyList<EtcdPermission> Permissions { get; init; } = [];

	/// <summary>
	/// Only root may list/modify users, roles and permissions.
	/// </summary>
	public bool CanManageAuth => IsRoot;

	public bool CanReadKeys => IsRoot || Permissions.Any(IsReadable);

	public bool CanWriteKeys => IsRoot || Permissions.Any(IsWritable);

	public bool CanReadKey(string key) => IsRoot || Permissions.Any(p => IsReadable(p) && p.Covers(key));

	public bool CanWriteKey(string key) => IsRoot || Permissions.Any(p => IsWritable(p) && p.Covers(key));

	/// <summary>
	/// etcd stores the "all keys" permission as the zero byte key, which means an empty prefix.
	/// </summary>
	public static string NormalizePrefix(string keyPrefix) => EtcdPermission.NormalizeKey(keyPrefix);

	private static bool IsReadable(EtcdPermission permission) =>
		permission.Type is PermissionType.Read or PermissionType.ReadWrite;

	private static bool IsWritable(EtcdPermission permission) =>
		permission.Type is PermissionType.Write or PermissionType.ReadWrite;
}
