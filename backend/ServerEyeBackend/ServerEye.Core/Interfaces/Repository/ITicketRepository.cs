namespace ServerEye.Core.Interfaces.Repository;

using ServerEye.Core.Entities;
using ServerEye.Core.Enums;

public interface ITicketRepository
{
    public Task<Ticket?> GetByIdAsync(Guid id);
    public Task<Ticket?> GetByTicketNumberAsync(string ticketNumber);
    public Task<List<Ticket>> GetAllAsync(int page = 1, int pageSize = 50);
    public Task<List<Ticket>> GetByStatusAsync(TicketStatus status, int page = 1, int pageSize = 50);
    public Task<List<Ticket>> GetByEmailAsync(string email);
    public Task<List<Ticket>> GetByUserIdAsync(Guid userId, int page = 1, int pageSize = 50);
    public Task<int> GetTotalCountAsync();
    public Task<int> GetCountByStatusAsync(TicketStatus status);
    public Task<Ticket> AddAsync(Ticket ticket);
    public Task UpdateAsync(Ticket ticket);
    public Task DeleteAsync(Guid id);
    public Task DeleteAsync(Ticket ticket);
}
