namespace EtcdTerminal.Presentation.Localization;

public interface ILocalizationCatalog
{
	IReadOnlyList<ILocalization> Localizations { get; }
	ILocalization Current { get; }

	/// <summary>
	/// Makes the language with the given code current. An unknown code falls
	/// back to the first language. Returns whether the language changed.
	/// </summary>
	bool Set(string? languageCode);
}