namespace ServerEye.Core.Interfaces.Repository;

using ServerEye.Core.Entities;

public interface ITicketMessageRepository
{
    public Task<TicketMessage?> GetByIdAsync(Guid id);
    public Task<List<TicketMessage>> GetByTicketIdAsync(Guid ticketId);
    public Task<TicketMessage> AddAsync(TicketMessage message);
    public Task DeleteAsync(Guid id);
    public Task DeleteByTicketIdAsync(Guid ticketId);
}
