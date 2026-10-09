using System.Text.Json.Nodes;
using EtcdTerminal.Configuration;

namespace EtcdTerminal.Infrastructure.Configuration;

public sealed class JsonBasedSettingsRepository(JsonConfigFile _configFile) : IAppSettingsRepository
{
	private const string SettingsSection = "Settings";
	private const string PageSizeProperty = "PageSize";
	private const string TrimInputValuesProperty = "TrimInputValues";
	private const string LanguageCodeProperty = "LanguageCode";
	private const string ThemeIdProperty = "ThemeId";

	public AppSettings Load()
	{
		if (_configFile.TryReadRoot() is not JsonObject jsonRoot)
			return new AppSettings();

		if (jsonRoot[SettingsSection] is not JsonObject settings)
			return new AppSettings();

		var appSettings = new AppSettings();

		if (settings[PageSizeProperty] is JsonValue pageSizeValue && pageSizeValue.TryGetValue<int>(out var pageSize) && pageSize >= 1)
			appSettings = appSettings with { PageSize = pageSize };

		if (settings[TrimInputValuesProperty] is JsonValue trimValue && trimValue.TryGetValue<bool>(out var trim))
			appSettings = appSettings with { TrimInputValues = trim };

		if (settings[LanguageCodeProperty] is JsonValue languageValue && languageValue.TryGetValue<string>(out var languageCode))
			appSettings = appSettings with { LanguageCode = languageCode };

		if (settings[ThemeIdProperty] is JsonValue themeValue && themeValue.TryGetValue<string>(out var themeId))
			appSettings = appSettings with { ThemeId = themeId };

		return appSettings;
	}

	public void Save(AppSettings appSettings)
	{
		var root = _configFile.ReadRootOrThrow();

		root[SettingsSection] = new JsonObject
		{
			[PageSizeProperty] = appSettings.PageSize,
			[TrimInputValuesProperty] = appSettings.TrimInputValues,
			[LanguageCodeProperty] = appSettings.LanguageCode,
			[ThemeIdProperty] = appSettings.ThemeId
		};

		_configFile.WriteRoot(root);
	}
}
