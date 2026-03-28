import { ticketApi } from '@/lib/ticketApi';

jest.mock('@/lib/api', () => ({
  apiClient: {
    get: jest.fn(),
    post: jest.fn(),
    put: jest.fn(),
    delete: jest.fn(),
  },
}));

// eslint-disable-next-line @typescript-eslint/no-require-imports
const { apiClient } = require('@/lib/api');

const makeTicket = (overrides = {}) => ({
  id: 'ticket-1',
  ticketNumber: 'TKT-001',
  name: 'Test User',
  email: 'test@example.com',
  userId: 'user-1',
  subject: 'Test Ticket',
  message: 'Ticket message',
  status: 'New',
  statusDisplay: 'New',
  priority: 'Medium',
  priorityDisplay: 'Medium',
  createdAt: '2024-01-01T00:00:00Z',
  updatedAt: null,
  resolvedAt: null,
  closedAt: null,
  assignedToUserName: null,
  messages: [],
  ...overrides,
});

describe('ticketApi', () => {
  beforeEach(() => {
    jest.clearAllMocks();
  });

  describe('createTicket', () => {
    it('creates a new ticket', async () => {
      const newTicket = makeTicket();
      apiClient.post.mockResolvedValue(newTicket);

      const request = {
        name: 'Test User',
        email: 'test@example.com',
        subject: 'Test Ticket',
        message: 'This is a test message',
      };

      const result = await ticketApi.createTicket(request);

      expect(apiClient.post).toHaveBeenCalledWith('/tickets', request);
      expect(result).toEqual(newTicket);
    });

    it('propagates errors from apiClient', async () => {
      apiClient.post.mockRejectedValue(new Error('Validation error'));

      await expect(
        ticketApi.createTicket({ name: '', email: '', subject: '', message: '' }),
      ).rejects.toThrow('Validation error');
    });
  });

  describe('getTicketByNumber', () => {
    it('retrieves a ticket by its ticket number', async () => {
      const ticket = makeTicket({ ticketNumber: 'TKT-100' });
      apiClient.get.mockResolvedValue(ticket);

      const result = await ticketApi.getTicketByNumber('TKT-100');

      expect(apiClient.get).toHaveBeenCalledWith('/tickets/number/TKT-100');
      expect(result.ticketNumber).toBe('TKT-100');
    });

    it('propagates errors for non-existent ticket number', async () => {
      apiClient.get.mockRejectedValue({ response: { status: 404 } });

      await expect(ticketApi.getTicketByNumber('INVALID')).rejects.toMatchObject({
        response: { status: 404 },
      });
    });
  });

  describe('getTicketById', () => {
    it('retrieves a ticket by its ID', async () => {
      const ticket = makeTicket({ id: 'abc-123' });
      apiClient.get.mockResolvedValue(ticket);

      const result = await ticketApi.getTicketById('abc-123');

      expect(apiClient.get).toHaveBeenCalledWith('/tickets/abc-123');
      expect(result.id).toBe('abc-123');
    });
  });

  describe('getTicketsByEmail', () => {
    it('retrieves tickets by email address', async () => {
      const tickets = [makeTicket({ email: 'user@example.com' })];
      apiClient.get.mockResolvedValue(tickets);

      const result = await ticketApi.getTicketsByEmail('user@example.com');

      expect(apiClient.get).toHaveBeenCalledWith('/tickets/email/user@example.com');
      expect(result).toHaveLength(1);
    });

    it('returns empty array when no tickets for email', async () => {
      apiClient.get.mockResolvedValue([]);

      const result = await ticketApi.getTicketsByEmail('noemail@example.com');

      expect(result).toEqual([]);
    });
  });

  describe('getTicketsByUserId', () => {
    it('retrieves tickets for a user with default pagination', async () => {
      const tickets = [makeTicket(), makeTicket({ id: 'ticket-2' })];
      apiClient.get.mockResolvedValue(tickets);

      const result = await ticketApi.getTicketsByUserId('user-1');

      expect(apiClient.get).toHaveBeenCalledWith('/tickets/user/user-1?page=1&pageSize=50');
      expect(Array.isArray(result)).toBe(true);
    });

    it('uses custom pagination parameters', async () => {
      apiClient.get.mockResolvedValue([]);

      await ticketApi.getTicketsByUserId('user-1', 2, 10);

      expect(apiClient.get).toHaveBeenCalledWith('/tickets/user/user-1?page=2&pageSize=10');
    });
  });

  describe('getTicketStats', () => {
    it('retrieves ticket statistics', async () => {
      const stats = { totalCount: 15, statusCounts: { New: 5, Open: 3, Resolved: 7 } };
      apiClient.get.mockResolvedValue(stats);

      const result = await ticketApi.getTicketStats();

      expect(apiClient.get).toHaveBeenCalledWith('/tickets/stats');
      expect(result).toEqual(stats);
    });
  });

  describe('getTicketsByStatus', () => {
    it('retrieves tickets filtered by status', async () => {
      const tickets = [makeTicket({ status: 'Open' })];
      apiClient.get.mockResolvedValue(tickets);

      const result = await ticketApi.getTicketsByStatus('Open' as any);

      expect(apiClient.get).toHaveBeenCalledWith('/tickets/status/Open');
      expect(result).toHaveLength(1);
    });
  });

  describe('updateTicketStatus', () => {
    it('updates the status of a ticket', async () => {
      const updatedTicket = makeTicket({ status: 'InProgress' });
      apiClient.put.mockResolvedValue(updatedTicket);

      const result = await ticketApi.updateTicketStatus('ticket-1', { status: 'InProgress' as any });

      expect(apiClient.put).toHaveBeenCalledWith('/tickets/ticket-1/status', { status: 'InProgress' });
      expect(result.status).toBe('InProgress');
    });

    it('propagates errors for non-existent ticket', async () => {
      apiClient.put.mockRejectedValue({ response: { status: 404 } });

      await expect(
        ticketApi.updateTicketStatus('nonexistent', { status: 'Resolved' as any }),
      ).rejects.toMatchObject({ response: { status: 404 } });
    });
  });

  describe('addTicketMessage', () => {
    it('adds a message to an existing ticket', async () => {
      const updatedTicket = makeTicket({
        messages: [
          {
            id: 'msg-1',
            message: 'Reply message',
            senderName: 'Support Staff',
            senderEmail: 'support@example.com',
            isStaffReply: true,
            createdAt: '2024-01-02T00:00:00Z',
          },
        ],
      });
      apiClient.post.mockResolvedValue(updatedTicket);

      const messageData = {
        message: 'Reply message',
        senderName: 'Support Staff',
        senderEmail: 'support@example.com',
        isStaffReply: true,
      };

      const result = await ticketApi.addTicketMessage('ticket-1', messageData as any);

      expect(apiClient.post).toHaveBeenCalledWith('/tickets/ticket-1/messages', messageData);
      expect(result.messages).toHaveLength(1);
      expect(result.messages[0].message).toBe('Reply message');
    });

    it('propagates errors for non-existent ticket', async () => {
      apiClient.post.mockRejectedValue({ response: { status: 404 } });

      await expect(
        ticketApi.addTicketMessage('nonexistent', { message: 'test' } as any),
      ).rejects.toMatchObject({ response: { status: 404 } });
    });
  });

  describe('getAllTickets', () => {
    it('retrieves all tickets with default pagination', async () => {
      const tickets = [makeTicket()];
      apiClient.get.mockResolvedValue(tickets);

      const result = await ticketApi.getAllTickets();

      expect(apiClient.get).toHaveBeenCalledWith('/tickets?page=1&pageSize=50');
      expect(result).toEqual(tickets);
    });

    it('uses custom pagination parameters', async () => {
      apiClient.get.mockResolvedValue({ tickets: [], pagination: {} });

      await ticketApi.getAllTickets(3, 25);

      expect(apiClient.get).toHaveBeenCalledWith('/tickets?page=3&pageSize=25');
    });
  });

  describe('deleteTicket', () => {
    it('deletes a ticket by ID', async () => {
      apiClient.delete.mockResolvedValue(undefined);

      await ticketApi.deleteTicket('ticket-to-delete');

      expect(apiClient.delete).toHaveBeenCalledWith('/tickets/ticket-to-delete');
    });

    it('propagates errors for non-existent ticket', async () => {
      apiClient.delete.mockRejectedValue({ response: { status: 404 } });

      await expect(ticketApi.deleteTicket('nonexistent')).rejects.toMatchObject({
        response: { status: 404 },
      });
    });

    it('propagates forbidden errors for non-admin users', async () => {
      apiClient.delete.mockRejectedValue({ response: { status: 403 } });

      await expect(ticketApi.deleteTicket('ticket-1')).rejects.toMatchObject({
        response: { status: 403 },
      });
    });
  });
});
