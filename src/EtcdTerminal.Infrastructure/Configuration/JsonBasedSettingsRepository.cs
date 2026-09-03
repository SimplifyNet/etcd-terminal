using System.Text.Json;
using System.Text.Json.Nodes;
using EtcdTerminal.Configuration;
using EtcdTerminal.Environment;

namespace EtcdTerminal.Infrastructure.Configuration;

public sealed class JsonBasedSettingsRepository(IAppEnvironment environment) : IAppSettingsRepository
{
	private const string SettingsSection = "Settings";
	private const string PageSizeProperty = "PageSize";
	private const string TrimInputValuesProperty = "TrimInputValues";

	private readonly string _configPath = environment.ConfigFilePath;
	private readonly JsonSerializerOptions _jsonOptions = new() { WriteIndented = true };

	public IAppSettings Load()
	{
		if (!File.Exists(_configPath))
			return new AppSettings();

		if ((JsonNode.Parse(File.ReadAllText(_configPath)) as JsonObject)?[SettingsSection] is not JsonObject settings)
			return new AppSettings();

		var appSettings = new AppSettings();

		if (settings[PageSizeProperty]?.GetValue<int>() is { } pageSize && pageSize >= 1)
			appSettings.PageSize = pageSize;

		if (settings[TrimInputValuesProperty]?.GetValue<bool>() is { } trim)
			appSettings.TrimInputValues = trim;

		return appSettings;
	}

	public void Save(IAppSettings appSettings)
	{
		var dir = Path.GetDirectoryName(_configPath)!;

		Directory.CreateDirectory(dir);

		JsonObject root;

		if (File.Exists(_configPath))
		{
			try
			{
				root = JsonNode.Parse(File.ReadAllText(_configPath)) as JsonObject ?? [];
			}
			catch
			{
				root = [];
			}
		}
		else
		{
			root = [];
		}

		root[SettingsSection] = new JsonObject
		{
			[PageSizeProperty] = appSettings.PageSize,
			[TrimInputValuesProperty] = appSettings.TrimInputValues
		};

		File.WriteAllText(_configPath, root.ToJsonString(_jsonOptions));
	}
}