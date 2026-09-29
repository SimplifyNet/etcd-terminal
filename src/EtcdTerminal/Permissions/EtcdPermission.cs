using System.Text;

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
	/// A zero-byte end means the range is open-ended: every key from the start on.
	/// </summary>
	public string RangeEnd { get; init; } = string.Empty;

	public PermissionScope Scope => PermissionRange.ScopeOf(KeyPrefix, RangeEnd);

	/// <summary>
	/// Display-safe form of <see cref="KeyPrefix"/>: control characters (such as the
	/// zero-byte sentinel) are replaced so raw bytes never reach the terminal.
	/// Display text is never used for requests; requests use the raw bounds.
	/// </summary>
	public string DisplayKey => DisplayText(KeyPrefix);

	/// <summary>
	/// Display-safe form of <see cref="RangeEnd"/>. See <see cref="DisplayKey"/>.
	/// </summary>
	public string DisplayRangeEnd => DisplayText(RangeEnd);

	/// <summary>
	/// Replaces control characters so a bound can be shown safely.
	/// </summary>
	private static string DisplayText(string bound) =>
		new([.. bound.Select(c => char.IsControl(c) ? '\uFFFD' : c)]);

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
			_ => CompareBytes(key, start) >= 0
				&& (RangeEnd == PermissionRange.AllKeys || CompareBytes(key, RangeEnd) < 0)
		};
	}

	private static int CompareBytes(string left, string right) =>
		Encoding.UTF8.GetBytes(left).AsSpan().SequenceCompareTo(Encoding.UTF8.GetBytes(right).AsSpan());
}
