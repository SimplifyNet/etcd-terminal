namespace EtcdTerminal.Permissions;

public sealed class EtcdPermission
{
	public PermissionType Type { get; init; }

	/// <summary>
	/// The permission key. It is a literal key when <see cref="Scope"/> is <see cref="PermissionScope.Key"/>,
	/// otherwise it is the start of the covered range.
	/// </summary>
	public string KeyPrefix { get; init; } = string.Empty;

	/// <summary>
	/// etcd range end. Empty means the permission covers exactly one key.
	/// </summary>
	public string RangeEnd { get; init; } = string.Empty;

	public PermissionScope Scope => PermissionRange.ScopeOf(KeyPrefix, RangeEnd);

	/// <summary>
	/// etcd stores the "all keys" permission as the zero byte key, which means an empty prefix.
	/// </summary>
	public static string NormalizeKey(string key) => key == PermissionRange.AllKeys ? string.Empty : key;

	public bool Covers(string key)
	{
		var start = NormalizeKey(KeyPrefix);

		return Scope switch
		{
			PermissionScope.Key => string.Equals(start, key, StringComparison.Ordinal),
			PermissionScope.Prefix => start.Length == 0 || key.StartsWith(start, StringComparison.Ordinal),
			_ => string.CompareOrdinal(key, start) >= 0 && string.CompareOrdinal(key, NormalizeKey(RangeEnd)) < 0
		};
	}
}
