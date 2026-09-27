using System.Text;

namespace EtcdTerminal.Permissions;

/// <summary>
/// etcd expresses permission width through the range end: an empty range end means a single key,
/// while the "next" key after the prefix means everything under that prefix.
/// Successors and bound comparisons use UTF-8 byte order, matching the server.
/// A successor is not always valid UTF-8 (e.g. for a prefix ending in U+D7FF),
/// so request bounds travel as bytes while the model keeps text.
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

		return Encoding.UTF8.GetString(PrefixRangeEndBytes(Encoding.UTF8.GetBytes(key)));
	}

	/// <summary>
	/// Byte-exact successor of <paramref name="key"/> for prefix requests and grants.
	/// An empty input, the zero byte, or a key of all 0xFF bytes yields the zero
	/// byte (unbounded tail).
	/// </summary>
	public static byte[] PrefixRangeEndBytes(byte[] key)
	{
		if (key.Length == 0 || (key.Length == 1 && key[0] == 0))
			return [0];

		var end = (byte[])key.Clone();

		for (var i = end.Length - 1; i >= 0; i--)
		{
			if (end[i] < 0xFF)
			{
				end[i]++;

				return end[..(i + 1)];
			}
		}

		return [0];
	}

	public static PermissionScope ScopeOf(string key, string rangeEnd)
	{
		if (string.IsNullOrEmpty(rangeEnd))
			return PermissionScope.Key;

		return rangeEnd == PrefixRangeEnd(key) ? PermissionScope.Prefix : PermissionScope.Range;
	}
}
