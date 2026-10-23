using EtcdTerminal.Keys;

namespace EtcdTerminal.App.Screens.Keys;

public sealed class KeyPager
{
	private IReadOnlyList<EtcdKeyValue> _source = [];
	private List<EtcdKeyValue> _filtered = [];

	public int FilteredCount => _filtered.Count;

	public void SetSource(IReadOnlyList<EtcdKeyValue> keys)
	{
		_source = keys;
		_filtered = [.. keys];
	}

	public void Filter(string query, bool caseSensitive)
	{
		var comparison = caseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;

		if (string.IsNullOrEmpty(query))
			_filtered = [.. _source];
		else
			_filtered = [.. _source.Where(kv =>
				kv.Key.Contains(query, comparison) ||
				kv.Value.Contains(query, comparison))];
	}

	public IReadOnlyList<EtcdKeyValue> GetPage(int page, int pageSize) =>
		[.. _filtered.Skip(page * pageSize).Take(pageSize)];

	public int GetTotalPages(int pageSize) =>
		_filtered.Count == 0 ? 1 : (int)Math.Ceiling((double)_filtered.Count / pageSize);
}
