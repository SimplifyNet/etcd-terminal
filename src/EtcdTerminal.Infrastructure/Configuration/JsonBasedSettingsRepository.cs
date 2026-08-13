using System.Text.Json;
using EtcdTerminal.Configuration;

namespace EtcdTerminal.Infrastructure.Configuration;

public sealed class JsonBasedSettingsRepository(IAppEnvironment environment) : IAppSettingsRepository
{
	private readonly string _settingsPath = environment.SettingsFilePath;
	private readonly JsonSerializerOptions _jsonOptions = new() { WriteIndented = true };

	public void Load()
	{
		if (!File.Exists(_settingsPath))
			return;

		try
		{
			var doc = JsonDocument.Parse(File.ReadAllText(_settingsPath));
			var root = doc.RootElement;

			if (root.TryGetProperty("PageSize", out var pageSizeElement) && pageSizeElement.TryGetInt32(out var pageSize) && pageSize >= 1)
				EtcdTerminalSettings.PageSize = pageSize;

			if (root.TryGetProperty("TrimInputValues", out var trimElement) && trimElement.ValueKind is JsonValueKind.True or JsonValueKind.False)
				EtcdTerminalSettings.TrimInputValues = trimElement.ValueKind == JsonValueKind.True;
		}
		catch
		{
		}
	}

	public void Save()
	{
		var dir = Path.GetDirectoryName(_settingsPath)!;

		Directory.CreateDirectory(dir);

		File.WriteAllText(_settingsPath, JsonSerializer.Serialize(new
		{
			EtcdTerminalSettings.PageSize,
			EtcdTerminalSettings.TrimInputValues
		}, _jsonOptions));
	}
}