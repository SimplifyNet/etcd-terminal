using EtcdTerminal.Presentation;

namespace EtcdTerminal.Tests;

/// <summary>
/// Concatenates the runs of one model line into the literal text it carries.
/// </summary>
internal static class LineText
{
	public static string Of(IReadOnlyList<StyledText> spans) =>
		string.Concat(spans.Select(span => span.Text));
}
