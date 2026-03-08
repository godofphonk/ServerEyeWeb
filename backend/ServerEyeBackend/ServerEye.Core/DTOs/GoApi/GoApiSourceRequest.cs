namespace ServerEye.Core.DTOs.GoApi;

using System.Text.Json.Serialization;

public class GoApiSourceRequest
{
    public string Source { get; set; } = string.Empty;
}

public class GoApiSourceResponse
{
    public string ServerId { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}

public class GoApiSourceIdentifiersRequest
{
    [JsonPropertyName("source_type")]
    public string SourceType { get; set; } = string.Empty;
    
    [JsonPropertyName("identifiers")]
    public List<string> Identifiers { get; set; } = new();
    
    [JsonPropertyName("identifier_type")]
    public string IdentifierType { get; set; } = string.Empty;
    
    [JsonPropertyName("metadata")]
    public Dictionary<string, object>? Metadata { get; set; }
    
    [JsonPropertyName("telegram_id")]
    public long? TelegramId { get; set; }
}

public class GoApiSourceIdentifiersResponse
{
    public string Message { get; set; } = string.Empty;
    public string ServerId { get; set; } = string.Empty;
    public string SourceType { get; set; } = string.Empty;
    public List<string> Identifiers { get; set; } = new();
    public string IdentifierType { get; set; } = string.Empty;
}
