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

		JsonNode? rootNode;

		try
		{
			rootNode = JsonNode.Parse(File.ReadAllText(_configPath));
		}
		catch (JsonException)
		{
			return new AppSettings();
		}

		if (rootNode as JsonObject is not JsonObject jsonRoot)
			return new AppSettings();

		if (jsonRoot[SettingsSection] is not JsonObject settings)
			return new AppSettings();

		var appSettings = new AppSettings();

		if (settings[PageSizeProperty] is JsonValue pageSizeValue && pageSizeValue.TryGetValue<int>(out var pageSize) && pageSize >= 1)
			appSettings.PageSize = pageSize;

		if (settings[TrimInputValuesProperty] is JsonValue trimValue && trimValue.TryGetValue<bool>(out var trim))
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

		JsonConfigFile.WriteAllTextAtomic(_configPath, root.ToJsonString(_jsonOptions));
	}
}
