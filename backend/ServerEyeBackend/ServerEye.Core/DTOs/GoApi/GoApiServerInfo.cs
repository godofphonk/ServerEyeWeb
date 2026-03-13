namespace ServerEye.Core.DTOs.GoApi;

using System.Text.Json.Serialization;

public class GoApiServerInfo
{
    [JsonPropertyName("id")]
    public string ServerId { get; init; } = string.Empty;

    [JsonPropertyName("server_key")]
    public string ServerKey { get; init; } = string.Empty;

    [JsonPropertyName("hostname")]
    public string Hostname { get; init; } = string.Empty;

    [JsonPropertyName("os_info")]
    public string OperatingSystem { get; init; } = string.Empty;

    [JsonPropertyName("agent_version")]
    public string AgentVersion { get; init; } = string.Empty;

    [JsonPropertyName("status")]
    public string Status { get; init; } = string.Empty;

    [JsonPropertyName("sources")]
    public string Sources { get; init; } = string.Empty;

    [JsonPropertyName("last_seen")]
    public DateTime? LastSeen { get; init; }

    [JsonPropertyName("created_at")]
    public DateTime? CreatedAt { get; init; }

    [JsonPropertyName("updated_at")]
    public DateTime? UpdatedAt { get; init; }
}
