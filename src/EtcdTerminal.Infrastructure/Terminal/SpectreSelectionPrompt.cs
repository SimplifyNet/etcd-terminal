using EtcdTerminal.Presentation;
using EtcdTerminal.Presentation.Localization;
using Spectre.Console;

namespace EtcdTerminal.Infrastructure.Terminal;

public sealed class SpectreSelectionPrompt(IAnsiConsole _console, RoleStyleMapper _styles, ILocalization _localization) : ISelectionPrompt
{
	private const int ReservedRows = 12;

	public Choice<TId>? Select<TId>(ChoiceList<TId> list)
	{
		var cancel = new Choice<TId>(default!, string.Empty);

		var prompt = new SelectionPrompt<Choice<TId>>()
			.AddChoices(list.Items)
			.UseConverter(choice => Markup.Escape(choice.Label))
			.HighlightStyle(_styles.Resolve(TextRole.Accent))
			.WrapAround(true)
			.PageSize(Math.Max(3, _console.Profile.Height - ReservedRows))
			.MoreChoicesText(Markup.Escape(_localization.MoreChoices))
			.AddCancelResult(cancel);

		if (!string.IsNullOrEmpty(list.Title))
			prompt.Title(Markup.Escape(list.Title));

		var selected = _console.Prompt(prompt);

		return ReferenceEquals(selected, cancel) ? null : selected;
	}
}
