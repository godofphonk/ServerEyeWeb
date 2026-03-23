namespace ServerEye.Infrastructure.ExternalServices.GoApi;

using System.Text.Json;
using Microsoft.Extensions.Logging;
using ServerEye.Core.DTOs.GoApi;

/// <summary>
/// JSON serialization/deserialization for Go API responses.
/// </summary>
public static class GoApiJsonSerializer
{
    private static ILogger? _logger;
    public static void SetLogger(ILogger logger)
    {
        _logger = logger;
    }

    private static readonly JsonSerializerOptions DefaultOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = null
    };

    private static readonly JsonSerializerOptions DebugOptions = new() { WriteIndented = true };

    /// <summary>
    /// Deserializes JSON content to specified type.
    /// </summary>
    public static T Deserialize<T>(string content)
    {
        return JsonSerializer.Deserialize<T>(content, DefaultOptions)!;
    }

    /// <summary>
    /// Attempts to deserialize without throwing exception.
    /// </summary>
    public static T? TryDeserialize<T>(string content)
    {
        try
        {
            return JsonSerializer.Deserialize<T>(content, DefaultOptions);
        }
        catch (JsonException)
        {
            return default;
        }
    }

    /// <summary>
    /// Deserializes Go API metrics response.
    /// </summary>
    public static GoApiMetricsResponse? DeserializeMetricsResponse(string content)
    {
        try
        {
            var result = JsonSerializer.Deserialize<GoApiMetricsResponse>(content, DefaultOptions);
            _logger?.LogDebug("Successfully deserialized {DataPointCount} data points", result?.DataPoints?.Count ?? 0);
            return result;
        }
        catch (JsonException ex)
        {
            _logger?.LogWarning(ex, "JSON deserialization failed. Content preview: {ContentPreview}", content[..Math.Min(500, content.Length)]);
            return null;
        }
    }

    /// <summary>
    /// Deserializes Go API snapshot response.
    /// </summary>
    public static GoApiSnapshotResponse? DeserializeSnapshotResponse(string content)
    {
        return TryDeserialize<GoApiSnapshotResponse>(content);
    }

    /// <summary>
    /// Deserializes Go API static info response.
    /// </summary>
    public static GoApiStaticInfoResponse? DeserializeStaticInfoResponse(string content)
    {
        return TryDeserialize<GoApiStaticInfoResponse>(content);
    }

    /// <summary>
    /// Deserializes Go API server info.
    /// </summary>
    public static GoApiServerInfo? DeserializeServerInfo(string content)
    {
        return TryDeserialize<GoApiServerInfo>(content);
    }

    /// <summary>
    /// Deserializes Go API server status.
    /// </summary>
    public static GoApiServerStatus? DeserializeServerStatus(string content)
    {
        return TryDeserialize<GoApiServerStatus>(content);
    }

    /// <summary>
    /// Deserializes Go API servers list.
    /// </summary>
    public static List<GoApiServerInfo>? DeserializeServersList(string content)
    {
        return TryDeserialize<List<GoApiServerInfo>>(content);
    }

    /// <summary>
    /// Deserializes Go API source response.
    /// </summary>
    public static GoApiSourceResponse? DeserializeSourceResponse(string content)
    {
        return TryDeserialize<GoApiSourceResponse>(content);
    }

    /// <summary>
    /// Deserializes Go API source identifiers response.
    /// </summary>
    public static GoApiSourceIdentifiersResponse? DeserializeSourceIdentifiersResponse(string content)
    {
        return TryDeserialize<GoApiSourceIdentifiersResponse>(content);
    }

    /// <summary>
    /// Deserializes Go API delete source response.
    /// </summary>
    public static GoApiDeleteSourceResponse? DeserializeDeleteSourceResponse(string content)
    {
        return TryDeserialize<GoApiDeleteSourceResponse>(content);
    }

    /// <summary>
    /// Serializes object to JSON for debugging.
    /// </summary>
    public static string SerializeForDebug<T>(T obj)
    {
        return JsonSerializer.Serialize(obj, DebugOptions);
    }
}
