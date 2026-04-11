import { apiClient } from '@/lib/api';
import {
  Ticket,
  CreateTicketRequest,
  CreateTicketResponse,
  TicketStatsResponse,
  AddTicketMessageRequest,
  UpdateTicketStatusRequest,
  TicketStatus,
  PaginatedTicketsResponse,
} from '@/types';

export class TicketApi {
  private readonly baseUrl = '/tickets';

  async createTicket(data: CreateTicketRequest): Promise<CreateTicketResponse> {
    return apiClient.post<CreateTicketResponse>(this.baseUrl, data);
  }

  async getTicketByNumber(ticketNumber: string): Promise<Ticket> {
    return apiClient.get<Ticket>(`${this.baseUrl}/number/${ticketNumber}`);
  }

  async getTicketById(ticketId: string): Promise<Ticket> {
    return apiClient.get<Ticket>(`${this.baseUrl}/${ticketId}`);
  }

  async getTicketsByEmail(email: string): Promise<Ticket[]> {
    return apiClient.get<Ticket[]>(`${this.baseUrl}/email/${email}`);
  }

  async getTicketsByUserId(
    userId: string,
    page: number = 1,
    pageSize: number = 50,
  ): Promise<Ticket[] | PaginatedTicketsResponse> {
    return apiClient.get<Ticket[] | PaginatedTicketsResponse>(
      `${this.baseUrl}/user/${userId}?page=${page}&pageSize=${pageSize}`,
    );
  }

  async getTicketStats(): Promise<TicketStatsResponse> {
    return apiClient.get<TicketStatsResponse>(`${this.baseUrl}/stats`);
  }

  async getTicketsByStatus(status: TicketStatus): Promise<Ticket[]> {
    return apiClient.get<Ticket[]>(`${this.baseUrl}/status/${status}`);
  }

  async updateTicketStatus(ticketId: string, data: UpdateTicketStatusRequest): Promise<Ticket> {
    return apiClient.put<Ticket>(`${this.baseUrl}/${ticketId}/status`, data);
  }

  async addTicketMessage(ticketId: string, data: AddTicketMessageRequest): Promise<Ticket> {
    return apiClient.post<Ticket>(`${this.baseUrl}/${ticketId}/messages`, data);
  }

  async getAllTickets(
    page: number = 1,
    pageSize: number = 50,
  ): Promise<
    Ticket[] | { tickets: Ticket[]; pagination: { page: number; pageSize: number; total: number } }
  > {
    return apiClient.get(`${this.baseUrl}?page=${page}&pageSize=${pageSize}`);
  }

  async deleteTicket(ticketId: string): Promise<void> {
    return apiClient.delete(`${this.baseUrl}/${ticketId}`);
  }
}

export const ticketApi = new TicketApi();
