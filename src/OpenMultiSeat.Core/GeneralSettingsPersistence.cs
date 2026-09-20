using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace OpenMultiSeat.Core;

public interface IGeneralSettingsPersistence
{
    Task<GeneralSettings> LoadAsync();
    Task SaveAsync(GeneralSettings settings);
}

/// <summary>Persists <see cref="GeneralSettings"/> as JSON, same on-disk location convention
/// (%AppData%\OpenMultiSeat\) as <see cref="SeatPersistence"/>, just a single object instead of a
/// per-seat collection since there's exactly one machine-wide settings record.</summary>
public class GeneralSettingsPersistence : IGeneralSettingsPersistence
{
    private const string SettingsFileName = "settings.json";
    private readonly string _configPath;
    private readonly ILogger<GeneralSettingsPersistence> _logger;
    private readonly JsonSerializerOptions _jsonOptions = new() { WriteIndented = true };

    public GeneralSettingsPersistence(ILogger<GeneralSettingsPersistence> logger)
    {
        _logger = logger;
        _configPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "OpenMultiSeat",
            SettingsFileName);
    }

    public async Task<GeneralSettings> LoadAsync()
    {
        try
        {
            if (File.Exists(_configPath))
            {
                var json = await File.ReadAllTextAsync(_configPath);
                return JsonSerializer.Deserialize<GeneralSettings>(json, _jsonOptions) ?? new GeneralSettings();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading general settings");
        }

        return new GeneralSettings();
    }

    public async Task SaveAsync(GeneralSettings settings)
    {
        try
        {
            var directory = Path.GetDirectoryName(_configPath);
            if (!string.IsNullOrEmpty(directory))
                Directory.CreateDirectory(directory);

            var json = JsonSerializer.Serialize(settings, _jsonOptions);
            await File.WriteAllTextAsync(_configPath, json);
            _logger.LogInformation($"General settings saved (StartMode={settings.StartMode})");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving general settings");
        }
    }
}
