using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;

namespace OpenMultiSeat.Core;

public interface ISeatPersistence
{
    Task<Seat?> GetSeatAsync(string seatId);
    Task<IReadOnlyList<Seat>> GetAllSeatsAsync();
    Task SaveSeatAsync(Seat seat);
    Task DeleteSeatAsync(string seatId);
}

public class SeatPersistence : ISeatPersistence
{
    private const string SeatConfigFileName = "seats.json";
    private readonly string _configPath;
    private readonly ILogger<SeatPersistence> _logger;
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = false
    };

    private Dictionary<string, Seat> _cache = [];
    private bool _isLoaded;

    public SeatPersistence(ILogger<SeatPersistence> logger)
    {
        _logger = logger;
        _configPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "OpenMultiSeat",
            SeatConfigFileName);
    }

    public async Task<Seat?> GetSeatAsync(string seatId)
    {
        await EnsureLoadedAsync();
        return _cache.TryGetValue(seatId, out var seat) ? seat : null;
    }

    public async Task<IReadOnlyList<Seat>> GetAllSeatsAsync()
    {
        await EnsureLoadedAsync();
        return _cache.Values.ToList();
    }

    public async Task SaveSeatAsync(Seat seat)
    {
        await EnsureLoadedAsync();

        if (!_cache.ContainsKey(seat.Id))
        {
            _logger.LogInformation($"New seat created: {seat.Id} ({seat.Name})");
        }

        _cache[seat.Id] = seat;
        await PersistAsync();
    }

    public async Task DeleteSeatAsync(string seatId)
    {
        await EnsureLoadedAsync();

        if (_cache.Remove(seatId))
        {
            _logger.LogInformation($"Seat deleted: {seatId}");
            await PersistAsync();
        }
    }

    private async Task EnsureLoadedAsync()
    {
        if (_isLoaded)
            return;

        try
        {
            if (File.Exists(_configPath))
            {
                var json = await File.ReadAllTextAsync(_configPath);
                var seats = JsonSerializer.Deserialize<List<Seat>>(json, _jsonOptions) ?? [];
                _cache = seats.ToDictionary(s => s.Id);
                _logger.LogInformation($"Loaded {_cache.Count} seat(s) from configuration");
            }
            else
            {
                _logger.LogInformation("No existing seat configuration found");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading seat configuration");
        }

        _isLoaded = true;
    }

    private async Task PersistAsync()
    {
        try
        {
            var directory = Path.GetDirectoryName(_configPath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var json = JsonSerializer.Serialize(_cache.Values.ToList(), _jsonOptions);
            await File.WriteAllTextAsync(_configPath, json);
            _logger.LogDebug($"Seat configuration persisted ({_cache.Count} seats)");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error persisting seat configuration");
        }
    }
}
