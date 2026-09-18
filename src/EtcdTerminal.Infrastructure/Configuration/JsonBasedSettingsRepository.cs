using System.Text.Json.Nodes;
using EtcdTerminal.Configuration;

namespace EtcdTerminal.Infrastructure.Configuration;

public sealed class JsonBasedSettingsRepository(JsonConfigFile _configFile) : IAppSettingsRepository
{
	private const string SettingsSection = "Settings";
	private const string PageSizeProperty = "PageSize";
	private const string TrimInputValuesProperty = "TrimInputValues";

	public IAppSettings Load()
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

		return appSettings;
	}

	public void Save(IAppSettings appSettings)
	{
		var root = _configFile.TryReadRoot() ?? [];

		root[SettingsSection] = new JsonObject
		{
			[PageSizeProperty] = appSettings.PageSize,
			[TrimInputValuesProperty] = appSettings.TrimInputValues
		};

		_configFile.WriteRoot(root);
	}
}