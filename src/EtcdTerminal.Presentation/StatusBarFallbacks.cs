namespace EtcdTerminal.Presentation;

/// <summary>
/// The order in which the footer loses session information when the terminal is
/// too narrow for everything: shorten the endpoint, then the user, then the
/// endpoint itself, then the hints, then the connection name. This is a
/// product decision about what the user can afford to lose, so it lives with
/// the model; whether a candidate actually fits is decided by the renderer,
/// which is the only one that knows the console width.
/// </summary>
public static class StatusBarFallbacks
{
	/// <summary>
	/// An endpoint shorter than this is not worth a place in the footer: it is
	/// dropped instead of being whittled down to a fragment.
	/// </summary>
	private const int EndpointTruncationWidth = 20;

	/// <summary>
	/// Appended to an endpoint that is being shortened field by field.
	/// </summary>
	private const string TruncationMark = "\u2026";

	/// <summary>
	/// Every possible footer content, from the complete one down to the version
	/// alone. The first one that fits the console width wins.
	/// </summary>
	public static IEnumerable<StatusBarModel> InPriorityOrder(StatusBarModel model)
	{
		yield return model;

		foreach (var shortened in Shorter(model.Connection))
			yield return model with { Connection = shortened };

		yield return model with { Username = null };

		foreach (var shortened in Shorter(model.Connection))
			yield return model with { Connection = shortened, Username = null };

		yield return model with { Connection = null, Username = null };
		yield return model with { Connection = null, Username = null, Hints = [] };
		yield return model with { Connection = null, Username = null, Hints = [], Name = null };
	}

	private static IEnumerable<StyledText> Shorter(StyledText? connection)
	{
		if (connection is null)
			yield break;

		var text = connection.Text;
		var cut = text.Length;

		while (cut > 0)
		{
			cut--;

			if (cut > 0 && char.IsLowSurrogate(text[cut]))
				cut--;

			var shortened = cut + TruncationMark.Length;

			if (shortened < EndpointTruncationWidth)
				yield break;

			if (shortened < text.Length)
				yield return connection with { Text = string.Concat(text.AsSpan(0, cut), TruncationMark) };
		}
	}
}
