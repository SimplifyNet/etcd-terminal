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
	public void ChineseLocalization_ProvidesTranslatedStrings()
	{
		var catalog = new LocalizationCatalog();

		Assert.That(catalog.Set("zh"), Is.True);

		var localization = catalog.Current;

		Assert.Multiple(() =>
		{
			Assert.That(localization, Is.InstanceOf<ChineseLocalization>());
			Assert.That(localization.Name, Is.EqualTo("简体中文"));
			Assert.That(localization.SettingsTitle, Is.EqualTo("设置"));
			Assert.That(localization.SelectLanguagePrompt, Is.EqualTo("选择语言："));
			Assert.That(localization.PageSizeLabel, Is.EqualTo("每页条数"));
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
