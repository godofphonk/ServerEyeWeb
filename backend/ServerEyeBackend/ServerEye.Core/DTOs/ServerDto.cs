namespace ServerEye.Core.DTOs;

using System.ComponentModel.DataAnnotations;

public class ServerDto
{
    [Required]
    [RegularExpression(@"^[a-f0-9-]{36}$", ErrorMessage = "Invalid server ID format")]
    public string Id { get; set; } = string.Empty;

    [Required]
    [StringLength(50, MinimumLength = 1, ErrorMessage = "Server name must be between 1 and 50 characters")]
    public string Name { get; set; } = string.Empty;

    [Required]
    [StringLength(255, MinimumLength = 1)]
    public string Hostname { get; set; } = string.Empty;

    [Required]
    [RegularExpression(@"^(?:[0-9]{1,3}\.){3}[0-9]{1,3}$", ErrorMessage = "Invalid IP address format")]
    public string IpAddress { get; set; } = string.Empty;

    [Required]
    [StringLength(50)]
    public string Os { get; set; } = string.Empty;

    [Required]
    [StringLength(20)]
    public string Status { get; set; } = string.Empty;

    // Removed ApiKey for security - should not be exposed in responses
    public DateTime LastHeartbeat { get; set; }

    [Required]
    public IReadOnlyCollection<string> Tags { get; set; } = [];

    [Required]
    public DateTime CreatedAt { get; set; }

    [Required]
    public DateTime UpdatedAt { get; set; }
}
