namespace ServerEye.Core.DTOs.GoApi;

using System.Text.Json.Serialization;

public class GoApiSourceRequest
{
    public string Source { get; init; } = string.Empty;
}

public class GoApiSourceResponse
{
    public string ServerId { get; init; } = string.Empty;
    public string Source { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
}

public class GoApiSourceIdentifiersRequest
{
    [JsonPropertyName("source_type")]
    public string SourceType { get; init; } = string.Empty;
    
    [JsonPropertyName("identifiers")]
    public List<string> Identifiers { get; init; } = new();
    
    [JsonPropertyName("identifier_type")]
    public string IdentifierType { get; init; } = string.Empty;
    
    [JsonPropertyName("metadata")]
    public Dictionary<string, object>? Metadata { get; init; }
    
    [JsonPropertyName("telegram_id")]
    public long? TelegramId { get; init; }
}

public class GoApiSourceIdentifiersResponse
{
    public string Message { get; init; } = string.Empty;
    public string ServerId { get; init; } = string.Empty;
    public string SourceType { get; init; } = string.Empty;
    public List<string> Identifiers { get; init; } = new();
    public string IdentifierType { get; init; } = string.Empty;
}
