namespace ServerEye.Core.Interfaces.Services;

using ServerEye.Core.DTOs.Ticket;
using ServerEye.Core.Enums;

public interface ITicketService
{
    public Task<TicketResponseDto> CreateTicketAsync(CreateTicketDto dto);
    public Task<TicketResponseDto?> GetTicketByIdAsync(Guid id);
    public Task<TicketResponseDto?> GetTicketByNumberAsync(string ticketNumber);
    public Task<List<TicketListItemDto>> GetAllTicketsAsync(int page = 1, int pageSize = 50);
    public Task<List<TicketListItemDto>> GetTicketsByStatusAsync(TicketStatus status, int page = 1, int pageSize = 50);
    public Task<List<TicketResponseDto>> GetTicketsByEmailAsync(string email);
    public Task<List<TicketListItemDto>> GetTicketsByUserIdAsync(Guid userId, int page = 1, int pageSize = 50);
    public Task<TicketResponseDto> UpdateTicketStatusAsync(Guid id, TicketStatus status);
    public Task<TicketMessageDto> AddMessageAsync(Guid ticketId, AddTicketMessageDto dto);
    public Task DeleteTicketAsync(Guid ticketId);
    public Task<int> GetTotalCountAsync();
    public Task<Dictionary<TicketStatus, int>> GetStatusCountsAsync();
}
