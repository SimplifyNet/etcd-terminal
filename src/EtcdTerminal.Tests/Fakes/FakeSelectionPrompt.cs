using EtcdTerminal.Presentation;

namespace EtcdTerminal.Tests.Fakes;

/// <summary>
/// Answers selection prompts from a scripted queue and records every choice
/// list it was offered, so a test asserts what was offered and what was chosen
/// instead of pressing keys.
/// </summary>
public sealed class FakeSelectionPrompt : ISelectionPrompt
{
	private readonly Queue<object?> _answers = new();

	/// Every choice list the prompt received, boxed because the id type differs
	/// per call; <see cref="Prompt{TId}"/> casts one back.
	public List<object> Prompts { get; } = [];

	public void Answer<TId>(TId id) where TId : notnull => _answers.Enqueue(id);

	public void Cancel() => _answers.Enqueue(null);

	public Choice<TId>? Select<TId>(ChoiceList<TId> list)
	{
		Prompts.Add(list);

		if (_answers.Count is 0)
			throw new InvalidOperationException("No answer was scripted for this selection prompt.");

		var answer = _answers.Dequeue();

		if (answer is null)
			return null;

		return list.Items.FirstOrDefault(item => Equals(item.Id, answer))
			?? throw new InvalidOperationException($"The prompt was never offered the id {answer}.");
	}

	public ChoiceList<TId> Prompt<TId>(int call) => (ChoiceList<TId>)Prompts[call];
}
