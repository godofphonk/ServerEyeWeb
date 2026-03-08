namespace ServerEye.Core.DTOs.GoApi;

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
    public string SourceType { get; set; } = string.Empty;
    public List<string> Identifiers { get; set; } = new();
    public string IdentifierType { get; set; } = string.Empty;
    public Dictionary<string, object>? Metadata { get; set; }
}

public class GoApiSourceIdentifiersResponse
{
    public string Message { get; set; } = string.Empty;
    public string ServerId { get; set; } = string.Empty;
    public string SourceType { get; set; } = string.Empty;
    public List<string> Identifiers { get; set; } = new();
    public string IdentifierType { get; set; } = string.Empty;
}
