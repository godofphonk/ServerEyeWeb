namespace ServerEye.Core.DTOs.GoApi;

using System.Text.Json.Serialization;

public class GoApiServerInfo
{
    [JsonPropertyName("server_id")]
    public string ServerId { get; init; } = string.Empty;

    [JsonPropertyName("server_key")]
    public string ServerKey { get; init; } = string.Empty;

    [JsonPropertyName("hostname")]
    public string Hostname { get; init; } = string.Empty;

    [JsonPropertyName("operating_system")]
    public string OperatingSystem { get; init; } = string.Empty;

    [JsonPropertyName("agent_version")]
    public string AgentVersion { get; init; } = string.Empty;

    [JsonPropertyName("last_seen")]
    public DateTime? LastSeen { get; init; }
}
