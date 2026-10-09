using EtcdTerminal.Presentation.Localization;

namespace EtcdTerminal.Tests.Fakes;

/// <summary>
/// A catalog that always answers with the one localization it was given.
/// </summary>
public sealed class FixedLocalizationCatalog(ILocalization _localization) : ILocalizationCatalog
{
	public IReadOnlyList<ILocalization> Localizations => [_localization];

	public ILocalization Current => _localization;

	public bool Set(string? languageCode) => false;
}
