using EtcdTerminal.Presentation.Localization;

namespace EtcdTerminal.App.Localization;

public sealed class LocalizationCatalog : ILocalizationCatalog
{
	private readonly IReadOnlyList<ILocalization> _localizations =
	[
		new EnglishLocalization(),
		new RussianLocalization(),
		new ChineseLocalization()
	];

	private ILocalization _current;

	public LocalizationCatalog() => _current = _localizations[0];

	public IReadOnlyList<ILocalization> Localizations => _localizations;
	public ILocalization Current => _current;

	public bool Set(string? languageCode)
	{
		var localization = _localizations.FirstOrDefault(item => item.LanguageCode == languageCode) ?? _localizations[0];

		if (ReferenceEquals(_current, localization))
			return false;

		_current = localization;

		return true;
	}
}
