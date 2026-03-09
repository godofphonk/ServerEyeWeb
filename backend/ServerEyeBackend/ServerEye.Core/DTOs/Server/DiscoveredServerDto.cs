namespace ServerEye.Core.DTOs.Server;

using System.Text.Json.Serialization;

public class DiscoveredServerDto
{
    [JsonPropertyName("server_id")]
    public string ServerId { get; init; } = string.Empty;

    [JsonPropertyName("hostname")]
    public string Hostname { get; init; } = string.Empty;

    [JsonPropertyName("operating_system")]
    public string OperatingSystem { get; init; } = string.Empty;

    [JsonPropertyName("last_seen")]
    public DateTime? LastSeen { get; init; }

    [JsonPropertyName("agent_version")]
    public string AgentVersion { get; init; } = string.Empty;

    [JsonPropertyName("added_via")]
    public string AddedVia { get; init; } = string.Empty;

    [JsonPropertyName("can_import")]
    public bool CanImport { get; init; }
}

public class DiscoveredServersResponseDto
{
    [JsonPropertyName("telegram_id")]
    public long TelegramId { get; init; }

    [JsonPropertyName("servers")]
    public List<DiscoveredServerDto> Servers { get; init; } = new();

    [JsonPropertyName("total_count")]
    public int TotalCount { get; init; }

    [JsonPropertyName("has_telegram_bot")]
    public bool HasTelegramBot { get; init; }

    [JsonPropertyName("telegram_bot_username")]
    public string? TelegramBotUsername { get; init; }
}

public class ImportServersRequestDto
{
    [JsonPropertyName("server_ids")]
    public List<string> ServerIds { get; init; } = new();
}

public class ImportServersResponseDto
{
    [JsonPropertyName("imported_count")]
    public int ImportedCount { get; init; }

    [JsonPropertyName("failed_count")]
    public int FailedCount { get; init; }

    [JsonPropertyName("servers")]
    public List<ServerResponse> Servers { get; init; } = new();

    [JsonPropertyName("errors")]
    public List<string> Errors { get; init; } = new();
}
