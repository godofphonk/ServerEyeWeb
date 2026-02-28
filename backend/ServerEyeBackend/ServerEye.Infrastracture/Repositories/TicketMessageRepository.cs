namespace ServerEye.Infrastracture.Repositories;

using Microsoft.EntityFrameworkCore;
using ServerEye.Core.Entities;
using ServerEye.Core.Interfaces.Repository;

public sealed class TicketMessageRepository : ITicketMessageRepository
{
    private readonly TicketDbContext context;

    public TicketMessageRepository(TicketDbContext context) =>
        this.context = context;

    public async Task<TicketMessage?> GetByIdAsync(Guid id) => await this.context
        .TicketMessages
        .AsNoTracking()
        .FirstOrDefaultAsync(tm => tm.Id == id)
        .ConfigureAwait(false);

    public async Task<List<TicketMessage>> GetByTicketIdAsync(Guid ticketId) => await this.context
        .TicketMessages
        .Where(tm => tm.TicketId == ticketId)
        .OrderBy(tm => tm.CreatedAt)
        .AsNoTracking()
        .ToListAsync()
        .ConfigureAwait(false);

    public async Task<TicketMessage> AddAsync(TicketMessage message)
    {
        await this.context.TicketMessages.AddAsync(message);
        await this.context.SaveChangesAsync();
        return message;
    }

    public async Task DeleteAsync(Guid id)
    {
        await this.context
            .TicketMessages
            .Where(tm => tm.Id == id)
            .ExecuteDeleteAsync();

        // ExecuteDeleteAsync already saves changes to database, no need for SaveChangesAsync()
    }

    public async Task DeleteByTicketIdAsync(Guid ticketId)
    {
        await this.context
            .TicketMessages
            .Where(tm => tm.TicketId == ticketId)
            .ExecuteDeleteAsync();

        // ExecuteDeleteAsync already saves changes to database, no need for SaveChangesAsync()
    }
}
