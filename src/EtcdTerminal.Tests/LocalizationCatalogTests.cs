using EtcdTerminal.App.Localization;
using NUnit.Framework;

namespace EtcdTerminal.Tests;

[TestFixture]
public sealed class LocalizationCatalogTests
{
	[Test]
	public void RussianLocalization_ProvidesTranslatedStrings()
	{
		var catalog = new LocalizationCatalog();

		catalog.Set("ru");

		var localization = catalog.Current;

		Assert.Multiple(() =>
		{
			Assert.That(localization.Name, Is.EqualTo("Русский"));
			Assert.That(localization.SettingsTitle, Is.EqualTo("Настройки"));
			Assert.That(localization.SelectThemePrompt, Is.EqualTo("Выберите тему:"));
			Assert.That(localization.PageSizeLabel, Is.EqualTo("Элементов на странице"));
		});
	}

	[Test]
	public void Set_ReportsWhetherTheSelectedLanguageChanged()
	{
		var catalog = new LocalizationCatalog();

		Assert.Multiple(() =>
		{
			Assert.That(catalog.Set("en"), Is.False);
			Assert.That(catalog.Set("ru"), Is.True);
			Assert.That(catalog.Current, Is.InstanceOf<RussianLocalization>());
		});
	}
}
