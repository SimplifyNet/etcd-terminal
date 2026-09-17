namespace EtcdTerminal.Permissions;

/// <summary>
/// etcd expresses permission width through the range end: an empty range end means a single key,
/// while the "next" key after the prefix means everything under that prefix.
/// </summary>
public static class PermissionRange
{
	/// <summary>
	/// etcd stores the "all keys" permission as the zero byte key with the zero byte range end.
	/// </summary>
	public const string AllKeys = "\0";

	/// <summary>
	/// Range end that turns <paramref name="key"/> into a prefix permission.
	/// </summary>
	public static string PrefixRangeEnd(string key)
	{
		if (key.Length == 0 || key == AllKeys)
			return AllKeys;

		for (var i = key.Length - 1; i >= 0; i--)
		{
			if (key[i] < char.MaxValue)
				return key[..i] + (char)(key[i] + 1);
		}

		return AllKeys;
	}

	/// <summary>
	/// Range end for the requested scope, or an empty string when the permission targets one key.
	/// </summary>
	public static string RangeEndFor(string key, PermissionScope scope) =>
		scope switch
		{
			PermissionScope.Prefix => PrefixRangeEnd(key),
			_ => string.Empty
		};

	public static PermissionScope ScopeOf(string key, string rangeEnd)
	{
		if (string.IsNullOrEmpty(rangeEnd))
			return PermissionScope.Key;

		return rangeEnd == PrefixRangeEnd(key) ? PermissionScope.Prefix : PermissionScope.Range;
	}
}
