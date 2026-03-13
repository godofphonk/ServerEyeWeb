namespace ServerEye.Core.DTOs.Server;

using System.Text.Json.Serialization;
using ServerEye.Core.DTOs.GoApi;

public class DeleteSourceRequestDto
{
    [JsonPropertyName("server_key")]
    public string ServerKey { get; init; } = string.Empty;
}

public class DeleteSourceResponseDto
{
    [JsonPropertyName("message")]
    public string Message { get; init; } = string.Empty;

    [JsonPropertyName("server_id")]
    public string ServerId { get; init; } = string.Empty;

    [JsonPropertyName("source")]
    public string Source { get; init; } = string.Empty;

    [JsonPropertyName("deleted_identifiers")]
    public List<string> DeletedIdentifiers { get; init; } = new();

    [JsonPropertyName("success")]
    public bool Success { get; init; }
}

public class DeleteSourceIdentifiersRequestDto
{
    [JsonPropertyName("identifiers")]
    public List<string> Identifiers { get; init; } = new();
}

public class DeleteSourceIdentifiersResponseDto
{
    [JsonPropertyName("message")]
    public string Message { get; init; } = string.Empty;

    [JsonPropertyName("server_id")]
    public string ServerId { get; init; } = string.Empty;

    [JsonPropertyName("source_type")]
    public string SourceType { get; init; } = string.Empty;

    [JsonPropertyName("deleted_identifiers")]
    public List<string> DeletedIdentifiers { get; init; } = new();

    [JsonPropertyName("success")]
    public bool Success { get; init; }
}
