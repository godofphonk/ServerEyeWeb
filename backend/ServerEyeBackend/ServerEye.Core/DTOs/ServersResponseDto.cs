namespace ServerEye.Core.DTOs;

public class ServersResponseDto
{
    public IReadOnlyCollection<ServerDto> Servers { get; set; } = [];
}
