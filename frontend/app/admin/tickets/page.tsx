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
      let data: Ticket[];
      
      if (filterStatus === 'all') {
        data = await ticketApi.getAllTickets();
      } else {
        data = await ticketApi.getTicketsByStatus(filterStatus);
      }
      
      setTickets(Array.isArray(data) ? data : []);
    } catch (error) {
      console.error('Failed to load tickets:', error);
      setTickets([]);
    }
  };

  const loadStats = async () => {
    try {
      const statsData = await ticketApi.getTicketStats();
      setStats(statsData);
    } catch (error) {
      console.error('Failed to load stats:', error);
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
