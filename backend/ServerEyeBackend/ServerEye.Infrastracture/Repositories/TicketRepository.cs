namespace ServerEye.Infrastracture.Repositories;

using Microsoft.EntityFrameworkCore;
using ServerEye.Core.Entities;
using ServerEye.Core.Enums;
using ServerEye.Core.Interfaces.Repository;

public sealed class TicketRepository : ITicketRepository
{
    private readonly TicketDbContext context;

    public TicketRepository(TicketDbContext context) =>
        this.context = context;

    public async Task<Ticket?> GetByIdAsync(Guid id) => await this.context
        .Tickets
        .Include(t => t.Messages)
        .Include(t => t.Attachments)
        .AsNoTracking()
        .FirstOrDefaultAsync(t => t.Id == id)
        .ConfigureAwait(false);

    public async Task<Ticket?> GetByTicketNumberAsync(string ticketNumber) => await this.context
        .Tickets
        .Include(t => t.Messages)
        .Include(t => t.Attachments)
        .AsNoTracking()
        .FirstOrDefaultAsync(t => t.TicketNumber == ticketNumber)
        .ConfigureAwait(false);

    public async Task<List<Ticket>> GetAllAsync(int page = 1, int pageSize = 50) => await this.context
        .Tickets
        .Include(t => t.Messages)
        .OrderByDescending(t => t.CreatedAt)
        .Skip((page - 1) * pageSize)
        .Take(pageSize)
        .AsNoTracking()
        .ToListAsync()
        .ConfigureAwait(false);

    public async Task<List<Ticket>> GetByStatusAsync(TicketStatus status, int page = 1, int pageSize = 50) => await this.context
        .Tickets
        .Include(t => t.Messages)
        .Where(t => t.Status == status)
        .OrderByDescending(t => t.CreatedAt)
        .Skip((page - 1) * pageSize)
        .Take(pageSize)
        .AsNoTracking()
        .ToListAsync()
        .ConfigureAwait(false);

    public async Task<List<Ticket>> GetByEmailAsync(string email) => await this.context
        .Tickets
        .Include(t => t.Messages)
        .Include(t => t.Attachments)
        .Where(t => t.Email == email)
        .OrderByDescending(t => t.CreatedAt)
        .AsNoTracking()
        .ToListAsync()
        .ConfigureAwait(false);

    public async Task<List<Ticket>> GetByUserIdAsync(Guid userId, int page = 1, int pageSize = 50) => await this.context
        .Tickets
        .Include(t => t.Messages)
        .Include(t => t.Attachments)
        .Where(t => t.UserId == userId)
        .OrderByDescending(t => t.CreatedAt)
        .Skip((page - 1) * pageSize)
        .Take(pageSize)
        .AsNoTracking()
        .ToListAsync()
        .ConfigureAwait(false);

    public async Task<int> GetTotalCountAsync() => await this.context
        .Tickets
        .CountAsync()
        .ConfigureAwait(false);

    public async Task<int> GetCountByStatusAsync(TicketStatus status) => await this.context
        .Tickets
        .CountAsync(t => t.Status == status)
        .ConfigureAwait(false);

    public async Task<Ticket> AddAsync(Ticket ticket)
    {
        await this.context.Tickets.AddAsync(ticket);
        await this.context.SaveChangesAsync();
        return ticket;
    }

    public async Task UpdateAsync(Ticket ticket)
    {
        this.context.Tickets.Update(ticket);
        await this.context.SaveChangesAsync();
    }

    public async Task DeleteAsync(Guid id)
    {
        await this.context
            .Tickets
            .Where(t => t.Id == id)
            .ExecuteDeleteAsync();
        await this.context.SaveChangesAsync();
    }
}
