using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace OpenMultiSeat.Core;

public interface IWorkplaceViewSettingsPersistence
{
    Task<WorkplaceViewSettings> LoadAsync();
    Task SaveAsync(WorkplaceViewSettings settings);
}

/// <summary>Persists <see cref="WorkplaceViewSettings"/> as JSON — same single-object,
/// %AppData%\OpenMultiSeat\ pattern as <see cref="GeneralSettingsPersistence"/>.</summary>
public class WorkplaceViewSettingsPersistence : IWorkplaceViewSettingsPersistence
{
    private const string SettingsFileName = "view-settings.json";
    private readonly string _configPath;
    private readonly ILogger<WorkplaceViewSettingsPersistence> _logger;
    private readonly JsonSerializerOptions _jsonOptions = new() { WriteIndented = true };

    public WorkplaceViewSettingsPersistence(ILogger<WorkplaceViewSettingsPersistence> logger)
    {
        _logger = logger;
        _configPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "OpenMultiSeat",
            SettingsFileName);
    }

    public async Task<WorkplaceViewSettings> LoadAsync()
    {
        try
        {
            if (File.Exists(_configPath))
            {
                var json = await File.ReadAllTextAsync(_configPath);
                return JsonSerializer.Deserialize<WorkplaceViewSettings>(json, _jsonOptions) ?? new WorkplaceViewSettings();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading workplace view settings");
        }

        return new WorkplaceViewSettings();
    }

    public async Task SaveAsync(WorkplaceViewSettings settings)
    {
        try
        {
            var directory = Path.GetDirectoryName(_configPath);
            if (!string.IsNullOrEmpty(directory))
                Directory.CreateDirectory(directory);

            settings.NewDeviceHighlightSeconds = WorkplaceViewSettings.ClampHighlightSeconds(settings.NewDeviceHighlightSeconds);

            var json = JsonSerializer.Serialize(settings, _jsonOptions);
            await File.WriteAllTextAsync(_configPath, json);
            _logger.LogInformation(
                $"Workplace view settings saved (ShowUnassignedDisplays={settings.ShowUnassignedDisplays}, NewDeviceHighlightSeconds={settings.NewDeviceHighlightSeconds})");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving workplace view settings");
        }
    }
}
