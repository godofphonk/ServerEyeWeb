'use client';

import { useEffect, useState } from 'react';
import { useRouter } from 'next/navigation';
import { useAuth } from '@/context/AuthContext';
import { Card, CardHeader, CardTitle, CardContent } from '@/components/ui/Card';
import { Button } from '@/components/ui/Button';
import { 
  Ticket as TicketIcon, 
  Clock, 
  AlertCircle, 
  CheckCircle, 
  XCircle, 
  Loader2,
  Filter,
  RefreshCw,
  MessageSquare,
  TrendingUp,
  Users
} from 'lucide-react';
import { ticketApi } from '@/lib/ticketApi';
import { Ticket, TicketStatus, TicketStatsResponse } from '@/types';
import { AdminTicketChat } from '@/components/admin/AdminTicketChat';
import { isAdmin } from '@/lib/auth';

const statusConfig = {
  [TicketStatus.New]: { icon: Clock, color: "text-blue-400", bg: "bg-blue-500/10", label: "New" },
  [TicketStatus.Open]: { icon: AlertCircle, color: "text-yellow-400", bg: "bg-yellow-500/10", label: "Open" },
  [TicketStatus.InProgress]: { icon: Loader2, color: "text-purple-400", bg: "bg-purple-500/10", label: "In Progress" },
  [TicketStatus.Resolved]: { icon: CheckCircle, color: "text-green-400", bg: "bg-green-500/10", label: "Resolved" },
  [TicketStatus.Closed]: { icon: XCircle, color: "text-gray-400", bg: "bg-gray-500/10", label: "Closed" },
  [TicketStatus.Reopened]: { icon: AlertCircle, color: "text-orange-400", bg: "bg-orange-500/10", label: "Reopened" }
};

export default function AdminTicketsPage() {
  const router = useRouter();
  const { user, isAuthenticated, loading: authLoading } = useAuth();
  const [tickets, setTickets] = useState<Ticket[]>([]);
  const [stats, setStats] = useState<TicketStatsResponse | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [selectedTicket, setSelectedTicket] = useState<Ticket | null>(null);
  const [isChatOpen, setIsChatOpen] = useState(false);
  const [filterStatus, setFilterStatus] = useState<TicketStatus | 'all'>('all');

  useEffect(() => {
    if (authLoading) return;

    console.log('[AdminTickets] User role check:', {
      isAuthenticated,
      userRole: user?.role,
      userTypeof: typeof user?.role,
      isAdmin: isAdmin(user)
    });

    if (!isAuthenticated || !isAdmin(user)) {
      console.log('[AdminTickets] Access denied - redirecting to dashboard');
      router.push('/dashboard');
      return;
    }

    console.log('[AdminTickets] Access granted - loading data');
    loadData();
  }, [authLoading, isAuthenticated, user, filterStatus]);

  const loadData = async () => {
    setIsLoading(true);
    try {
      await Promise.all([
        loadTickets(),
        loadStats()
      ]);
    } finally {
      setIsLoading(false);
    }
  };

  const loadTickets = async () => {
    try {
      console.log('[AdminTickets] Loading tickets with filter:', filterStatus);
      
      let data: Ticket[];
      
      if (filterStatus === 'all') {
        console.log('[AdminTickets] Calling getAllTickets()');
        const response = await ticketApi.getAllTickets();
        console.log('[AdminTickets] getAllTickets response:', response);
        console.log('[AdminTickets] getAllTickets type:', typeof response);
        console.log('[AdminTickets] getAllTickets isArray?:', Array.isArray(response));
        
        // Handle paginated response {tickets: [...], pagination: {...}}
        if (response && response.tickets && Array.isArray(response.tickets)) {
          data = response.tickets;
          console.log('[AdminTickets] Extracted tickets from paginated response:', data.length);
        } else if (Array.isArray(response)) {
          data = response;
        } else {
          data = [];
        }
      } else {
        console.log('[AdminTickets] Calling getTicketsByStatus with status:', filterStatus);
        data = await ticketApi.getTicketsByStatus(filterStatus);
        console.log('[AdminTickets] getTicketsByStatus response:', data);
      }
      
      let ticketsArray = Array.isArray(data) ? data : [];
      console.log('[AdminTickets] Setting tickets:', ticketsArray.length, 'items');
      
      // Smart sorting logic:
      // 1. New/Reopened tickets first (need attention)
      // 2. In Progress tickets (being worked on)
      // 3. Open tickets (waiting)
      // 4. Resolved tickets (completed but not closed)
      // 5. Closed tickets last
      const statusPriority = {
        [TicketStatus.New]: 1,
        [TicketStatus.Reopened]: 2,
        [TicketStatus.InProgress]: 3,
        [TicketStatus.Open]: 4,
        [TicketStatus.Resolved]: 5,
        [TicketStatus.Closed]: 6
      };

      ticketsArray = ticketsArray.sort((a, b) => {
        // First sort by status priority
        const statusDiff = statusPriority[a.status] - statusPriority[b.status];
        if (statusDiff !== 0) return statusDiff;
        
        // Then by creation date (newest first within same status)
        return new Date(b.createdAt).getTime() - new Date(a.createdAt).getTime();
      });

      console.log('[AdminTickets] Tickets sorted by priority');
      
      // If no tickets from getAllTickets, try fallback to admin's own tickets
      if (ticketsArray.length === 0 && user) {
        console.log('[AdminTickets] No tickets from getAllTickets, trying fallback to admin user tickets...');
        try {
          const adminTickets = await ticketApi.getTicketsByUserId(user.id, 1, 50);
          const adminTicketsArray = Array.isArray(adminTickets) ? adminTickets : [];
          console.log('[AdminTickets] Admin fallback tickets:', adminTicketsArray.length);
          setTickets(adminTicketsArray);
        } catch (fallbackErr) {
          console.error('[AdminTickets] Fallback also failed:', fallbackErr);
          setTickets(ticketsArray);
        }
      } else {
        setTickets(ticketsArray);
      }
    } catch (error: any) {
      console.error('[AdminTickets] Failed to load tickets:', error);
      console.error('[AdminTickets] Error details:', {
        message: error.message,
        status: error.status,
        statusText: error.statusText,
        response: error.response
      });
      setTickets([]);
    }
  };

  const loadStats = async () => {
    try {
      console.log('[AdminTickets] Loading ticket stats...');
      const statsData = await ticketApi.getTicketStats();
      console.log('[AdminTickets] Stats loaded:', statsData);
      setStats(statsData);
    } catch (error: any) {
      console.error('[AdminTickets] Failed to load stats:', error);
      console.error('[AdminTickets] Stats error details:', {
        message: error.message,
        status: error.status,
        response: error.response
      });
    }
  };

  const openChat = (ticket: Ticket) => {
    setSelectedTicket(ticket);
    setIsChatOpen(true);
  };

  const closeChat = () => {
    setIsChatOpen(false);
    setSelectedTicket(null);
  };

  const handleTicketUpdate = () => {
    loadData();
  };

  const formatDate = (dateString: string) => {
    return new Date(dateString).toLocaleString();
  };

  if (authLoading || isLoading) {
    return (
      <div className="min-h-screen bg-black flex items-center justify-center">
        <Loader2 className="w-8 h-8 animate-spin text-blue-400" />
      </div>
    );
  }

  return (
    <div className="min-h-screen bg-black text-white p-6">
      <div className="max-w-7xl mx-auto space-y-6">
        {/* Header */}
        <div className="flex items-center justify-between">
          <div>
            <h1 className="text-3xl font-bold">Ticket Management</h1>
            <p className="text-gray-400 mt-1">Manage and respond to support tickets</p>
          </div>
          <Button
            variant="ghost"
            size="sm"
            onClick={loadData}
            className="gap-2"
          >
            <RefreshCw className="w-4 h-4" />
            Refresh
          </Button>
        </div>

        {/* Stats Cards */}
        {stats && (
          <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-4">
            <Card className="bg-gray-900 border-blue-500/20">
              <CardContent className="p-6">
                <div className="flex items-center justify-between">
                  <div>
                    <p className="text-sm text-gray-400">Total Tickets</p>
                    <p className="text-2xl font-bold mt-1">{stats.total}</p>
                  </div>
                  <TicketIcon className="w-8 h-8 text-blue-400" />
                </div>
              </CardContent>
            </Card>

            <Card className="bg-gray-900 border-yellow-500/20">
              <CardContent className="p-6">
                <div className="flex items-center justify-between">
                  <div>
                    <p className="text-sm text-gray-400">Open</p>
                    <p className="text-2xl font-bold mt-1">{stats.open}</p>
                  </div>
                  <AlertCircle className="w-8 h-8 text-yellow-400" />
                </div>
              </CardContent>
            </Card>

            <Card className="bg-gray-900 border-purple-500/20">
              <CardContent className="p-6">
                <div className="flex items-center justify-between">
                  <div>
                    <p className="text-sm text-gray-400">In Progress</p>
                    <p className="text-2xl font-bold mt-1">{stats.inProgress}</p>
                  </div>
                  <TrendingUp className="w-8 h-8 text-purple-400" />
                </div>
              </CardContent>
            </Card>

            <Card className="bg-gray-900 border-green-500/20">
              <CardContent className="p-6">
                <div className="flex items-center justify-between">
                  <div>
                    <p className="text-sm text-gray-400">Resolved</p>
                    <p className="text-2xl font-bold mt-1">{stats.resolved}</p>
                  </div>
                  <CheckCircle className="w-8 h-8 text-green-400" />
                </div>
              </CardContent>
            </Card>
          </div>
        )}

        {/* Filters */}
        <Card>
          <CardHeader>
            <div className="flex items-center gap-2">
              <Filter className="w-5 h-5" />
              <CardTitle>Filter by Status</CardTitle>
            </div>
          </CardHeader>
          <CardContent>
            <div className="flex flex-wrap gap-2">
              <Button
                variant={filterStatus === 'all' ? 'primary' : 'ghost'}
                size="sm"
                onClick={() => setFilterStatus('all')}
              >
                All
              </Button>
              {Object.entries(statusConfig).map(([status, config]) => {
                const StatusIcon = config.icon;
                return (
                  <Button
                    key={status}
                    variant={filterStatus === Number(status) ? 'primary' : 'ghost'}
                    size="sm"
                    onClick={() => setFilterStatus(Number(status) as TicketStatus)}
                    className="gap-2"
                  >
                    <StatusIcon className="w-4 h-4" />
                    {config.label}
                  </Button>
                );
              })}
            </div>
          </CardContent>
        </Card>

        {/* Tickets List */}
        <Card>
          <CardHeader>
            <CardTitle>Tickets ({tickets.length})</CardTitle>
          </CardHeader>
          <CardContent>
            {tickets.length === 0 ? (
              <div className="text-center py-12">
                <TicketIcon className="w-12 h-12 text-gray-400 mx-auto mb-4" />
                <p className="text-gray-400">No tickets found</p>
              </div>
            ) : (
              <div className="space-y-3">
                {tickets.map((ticket) => {
                  const config = statusConfig[ticket.status];
                  const StatusIcon = config.icon;

                  return (
                    <div
                      key={ticket.id}
                      className="bg-gray-800/50 border border-white/10 rounded-lg p-4 hover:border-blue-500/30 transition-colors"
                    >
                      <div className="flex items-start justify-between gap-4">
                        <div className="flex-1 min-w-0">
                          <div className="flex items-center gap-3 mb-2">
                            <span className="text-sm text-gray-400">#{ticket.ticketNumber}</span>
                            <div className={`flex items-center gap-1 px-2 py-1 rounded-full text-xs ${config.bg} ${config.color}`}>
                              <StatusIcon className="w-3 h-3" />
                              {config.label}
                            </div>
                            <span className="text-xs text-gray-500">{ticket.priorityDisplay}</span>
                          </div>
                          
                          <h3 className="font-semibold text-lg mb-1">{ticket.subject}</h3>
                          <p className="text-sm text-gray-400 line-clamp-2 mb-3">{ticket.message}</p>
                          
                          <div className="flex items-center gap-4 text-xs text-gray-500">
                            <span className="flex items-center gap-1">
                              <Users className="w-3 h-3" />
                              {ticket.name} ({ticket.email})
                            </span>
                            <span>{formatDate(ticket.createdAt)}</span>
                            {(ticket.messagesCount || 0) > 0 && (
                              <span className="flex items-center gap-1">
                                <MessageSquare className="w-3 h-3" />
                                {ticket.messagesCount || 0} {(ticket.messagesCount || 0) === 1 ? 'message' : 'messages'}
                              </span>
                            )}
                          </div>
                        </div>

                        <Button
                          variant="primary"
                          size="sm"
                          onClick={() => openChat(ticket)}
                          className="gap-2 flex-shrink-0"
                        >
                          <MessageSquare className="w-4 h-4" />
                          Respond
                        </Button>
                      </div>
                    </div>
                  );
                })}
              </div>
            )}
          </CardContent>
        </Card>
      </div>

      {/* Admin Chat Modal */}
      {selectedTicket && (
        <AdminTicketChat
          ticket={selectedTicket}
          isOpen={isChatOpen}
          onClose={closeChat}
          onTicketUpdate={handleTicketUpdate}
        />
      )}
    </div>
  );
}
