namespace EtcdTerminal.App.Components;

/// <summary>
/// The filter and the page window of a browsable list. The caller supplies
/// the source and the match text of a row; the pager only slices and counts,
/// it never measures or draws.
/// </summary>
public sealed class ListPager<T>(IReadOnlyList<T> source, Func<T, string> match)
{
	private List<T> _filtered = [.. source];

	public int FilteredCount => _filtered.Count;

	public void Filter(string query) =>
		_filtered = string.IsNullOrEmpty(query)
			? [.. source]
			: [.. source.Where(item => match(item).Contains(query, StringComparison.OrdinalIgnoreCase))];

	public IReadOnlyList<T> GetPage(int page, int pageSize) =>
		[.. _filtered.Skip(page * pageSize).Take(pageSize)];

	public int GetTotalPages(int pageSize) =>
		_filtered.Count == 0 ? 1 : (int)Math.Ceiling((double)_filtered.Count / pageSize);
}
