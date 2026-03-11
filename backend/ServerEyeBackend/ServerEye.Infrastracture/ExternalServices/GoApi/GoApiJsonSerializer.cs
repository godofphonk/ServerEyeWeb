namespace ServerEye.Infrastracture.ExternalServices.GoApi;

using System.Text.Json;
using ServerEye.Core.DTOs.GoApi;

/// <summary>
/// JSON serialization/deserialization for Go API responses.
/// </summary>
public class GoApiJsonSerializer
{
    private static readonly JsonSerializerOptions DefaultOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = null
    };

    private static readonly JsonSerializerOptions DebugOptions = new() { WriteIndented = true };

    /// <summary>
    /// Deserializes JSON content to specified type.
    /// </summary>
    public T? Deserialize<T>(string content)
    {
        return JsonSerializer.Deserialize<T>(content, DefaultOptions);
    }

    /// <summary>
    /// Attempts to deserialize without throwing exception.
    /// </summary>
    public T? TryDeserialize<T>(string content)
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
    public GoApiMetricsResponse? DeserializeMetricsResponse(string content)
    {
        return TryDeserialize<GoApiMetricsResponse>(content);
    }

    /// <summary>
    /// Deserializes Go API snapshot response.
    /// </summary>
    public GoApiSnapshotResponse? DeserializeSnapshotResponse(string content)
    {
        return TryDeserialize<GoApiSnapshotResponse>(content);
    }

    /// <summary>
    /// Deserializes Go API static info response.
    /// </summary>
    public GoApiStaticInfoResponse? DeserializeStaticInfoResponse(string content)
    {
        return TryDeserialize<GoApiStaticInfoResponse>(content);
    }

    /// <summary>
    /// Deserializes Go API server info.
    /// </summary>
    public GoApiServerInfo? DeserializeServerInfo(string content)
    {
        return TryDeserialize<GoApiServerInfo>(content);
    }

    /// <summary>
    /// Deserializes Go API servers list.
    /// </summary>
    public List<GoApiServerInfo>? DeserializeServersList(string content)
    {
        return TryDeserialize<List<GoApiServerInfo>>(content);
    }

    /// <summary>
    /// Deserializes Go API source response.
    /// </summary>
    public GoApiSourceResponse? DeserializeSourceResponse(string content)
    {
        return TryDeserialize<GoApiSourceResponse>(content);
    }

    /// <summary>
    /// Deserializes Go API source identifiers response.
    /// </summary>
    public GoApiSourceIdentifiersResponse? DeserializeSourceIdentifiersResponse(string content)
    {
        return TryDeserialize<GoApiSourceIdentifiersResponse>(content);
    }

    /// <summary>
    /// Serializes object to JSON for debugging.
    /// </summary>
    public static string SerializeForDebug<T>(T obj)
    {
        return JsonSerializer.Serialize(obj, DebugOptions);
    }
}
