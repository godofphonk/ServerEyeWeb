namespace ServerEye.Core.DTOs;

using System.Text.Json.Serialization;

public class ErrorResponseDto
{
    [JsonPropertyName("error")]
    public string Error { get; init; } = string.Empty;

    [JsonPropertyName("message")]
    public string Message { get; init; } = string.Empty;

    [JsonPropertyName("user_message")]
    public string? UserMessage { get; init; }

    [JsonPropertyName("error_code")]
    public string? ErrorCode { get; init; }

    [JsonPropertyName("support_contact")]
    public string? SupportContact { get; init; }

    [JsonPropertyName("timestamp")]
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;

    [JsonPropertyName("details")]
    public Dictionary<string, object>? Details { get; init; }
}
