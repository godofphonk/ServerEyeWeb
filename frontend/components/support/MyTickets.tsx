'use client';

import { useState, useEffect } from 'react';
import { motion, AnimatePresence } from 'framer-motion';
import {
  FileText,
  Clock,
  AlertCircle,
  CheckCircle,
  XCircle,
  Loader2,
  MessageSquare,
  RefreshCw,
  MessageCircle,
  Plus,
} from 'lucide-react';
import { Card, CardHeader, CardTitle, CardContent } from '@/components/ui/Card';
import { Button } from '@/components/ui/Button';
import { ticketApi } from '@/lib/ticketApi';
import { Ticket, TicketStatus } from '@/types';
import { useAuth } from '@/context/AuthContext';
import { TicketChat } from './TicketChat';

const statusConfig = {
  [TicketStatus.New]: {
    icon: Clock,
    color: 'text-blue-400',
    bg: 'bg-blue-500/10',
    border: 'border-blue-500/20',
  },
  [TicketStatus.Open]: {
    icon: AlertCircle,
    color: 'text-yellow-400',
    bg: 'bg-yellow-500/10',
    border: 'border-yellow-500/20',
  },
  [TicketStatus.InProgress]: {
    icon: Loader2,
    color: 'text-purple-400',
    bg: 'bg-purple-500/10',
    border: 'border-purple-500/20',
  },
  [TicketStatus.Resolved]: {
    icon: CheckCircle,
    color: 'text-green-400',
    bg: 'bg-green-500/10',
    border: 'border-green-500/20',
  },
  [TicketStatus.Closed]: {
    icon: XCircle,
    color: 'text-gray-400',
    bg: 'bg-gray-500/10',
    border: 'border-gray-500/20',
  },
  [TicketStatus.Reopened]: {
    icon: AlertCircle,
    color: 'text-orange-400',
    bg: 'bg-orange-500/10',
    border: 'border-orange-500/20',
  },
};

export function MyTickets() {
  const { user, isAuthenticated } = useAuth();
  const [tickets, setTickets] = useState<Ticket[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [expandedTicket, setExpandedTicket] = useState<string | null>(null);
  const [selectedTicket, setSelectedTicket] = useState<Ticket | null>(null);
  const [isChatOpen, setIsChatOpen] = useState(false);
  const [pagination, setPagination] = useState({
    page: 1,
    pageSize: 50,
    totalCount: 0,
    totalPages: 0,
    hasNextPage: false,
    hasPreviousPage: false,
  });

  useEffect(() => {
    if (isAuthenticated && user) {
      loadTickets();
    } else if (!isAuthenticated) {
      setError('Please log in to view your tickets');
      setIsLoading(false);
    }
  }, [isAuthenticated, user]);

  const loadTickets = async (page: number = 1) => {
    if (!user) return;

    setIsLoading(true);
    setError(null);

    try {
      const result = await ticketApi.getTicketsByUserId(user.id, page, pagination.pageSize);

      // Backend returns array directly, not paginated object
      if (Array.isArray(result)) {
        setTickets(result);
        setPagination({
          page: 1,
          pageSize: 50,
          totalCount: result.length,
          totalPages: 1,
          hasNextPage: false,
          hasPreviousPage: false,
        });
      } else if (result && result.tickets) {
        // Paginated response format
        setTickets(result.tickets);
        setPagination({
          page: result.page,
          pageSize: result.pageSize,
          totalCount: result.totalCount,
          totalPages: result.totalPages,
          hasNextPage: result.hasNextPage,
          hasPreviousPage: result.hasPreviousPage,
        });
      } else {
        // Fallback to email-based loading
        try {
          const emailResult = await ticketApi.getTicketsByEmail(user.email);
          setTickets(Array.isArray(emailResult) ? emailResult : []);
        } catch (_emailErr) {
          setTickets([]);
        }
      }
    } catch (_err: unknown) {
       
      setError('Failed to load tickets');
      setTickets([]);
    } finally {
      setIsLoading(false);
    }
  };

  const handlePageChange = (newPage: number) => {
    if (newPage >= 1 && newPage <= pagination.totalPages) {
      loadTickets(newPage);
    }
  };

  // Debug function to create a test ticket
  const createTestTicket = async () => {
    if (!user) return;

    try {
      // eslint-disable-next-line @typescript-eslint/no-unused-vars
      const testTicket = await ticketApi.createTicket({
        name: user.username,
        email: user.email,
        subject: `Test Ticket ${Date.now()}`,
        message: 'This is a test ticket created for debugging purposes.',
      });

      // Reload tickets after creating
      loadTickets();
    } catch (_err: unknown) {

      /* ignore error */
    }
  };

  const _toggleTicketExpansion = (ticketId: string) => {
    setExpandedTicket(expandedTicket === ticketId ? null : ticketId);
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
    loadTickets(pagination.page);
  };

  if (isLoading) {
    return (
      <Card>
        <CardContent className='flex items-center justify-center py-12'>
          <Loader2 className='w-6 h-6 animate-spin text-blue-400' />
        </CardContent>
      </Card>
    );
  }

  if (error) {
    return (
      <Card>
        <CardContent className='flex items-center justify-center py-12'>
          <div className='text-center'>
            <AlertCircle className='w-12 h-12 text-red-400 mx-auto mb-4' />
            <p className='text-red-400'>{error}</p>
          </div>
        </CardContent>
      </Card>
    );
  }

  if (!tickets || tickets.length === 0) {
    return (
      <Card>
        <CardHeader>
          <div className='flex items-center justify-between'>
            <CardTitle className='text-lg'>My Tickets</CardTitle>
            <Button
              variant='ghost'
              size='sm'
              onClick={() => loadTickets()}
              disabled={isLoading}
              className='gap-2'
            >
              <RefreshCw className={`w-4 h-4 ${isLoading ? 'animate-spin' : ''}`} />
              Refresh
            </Button>
          </div>
        </CardHeader>
        <CardContent className='flex items-center justify-center py-12'>
          <div className='text-center'>
            <FileText className='w-12 h-12 text-gray-400 mx-auto mb-4' />
            <p className='text-gray-400'>No tickets found</p>
            <p className='text-sm text-gray-500 mt-2'>Create your first support ticket</p>
          </div>
        </CardContent>
      </Card>
    );
  }

  return (
    <>
      <Card>
        <CardHeader>
          <div className='flex items-center justify-between'>
            <CardTitle className='text-lg'>My Tickets</CardTitle>
            <div className='flex gap-2'>
              <Button
                variant='ghost'
                size='sm'
                onClick={() => loadTickets()}
                disabled={isLoading}
                className='gap-2'
              >
                <RefreshCw className={`w-4 h-4 ${isLoading ? 'animate-spin' : ''}`} />
                Refresh
              </Button>
              {/* Debug button - remove in production */}
              <Button
                variant='ghost'
                size='sm'
                onClick={createTestTicket}
                className='gap-2 text-xs'
              >
                <Plus className='w-3 h-3' />
                Test Ticket
              </Button>
            </div>
          </div>
        </CardHeader>
        <CardContent className='p-0'>
          <div className='space-y-4'>
            {tickets?.map((ticket, index) => {
              const config = statusConfig[ticket.status];
              const StatusIcon = config.icon;
              const isExpanded = expandedTicket === ticket.id;

              return (
                <motion.div
                  key={ticket.id}
                  initial={{ opacity: 0, y: 20 }}
                  animate={{ opacity: 1, y: 0 }}
                  transition={{ delay: index * 0.05 }}
                >
                  <Card className='hover:bg-white/5 transition-colors'>
                    <CardContent className='p-4'>
                      <div className='flex items-start justify-between'>
                        <div className='flex-1'>
                          <div className='flex items-center gap-3 mb-2'>
                            <StatusIcon className={`w-4 h-4 ${config.color}`} />
                            <span className='font-mono text-sm text-gray-400'>
                              {ticket.ticketNumber}
                            </span>
                            <span
                              className={`text-xs font-semibold px-2 py-1 rounded-full ${config.bg} ${config.border} ${config.color}`}
                            >
                              {ticket.statusDisplay}
                            </span>
                            <span className='text-xs text-gray-500'>
                              Priority: {ticket.priorityDisplay}
                            </span>
                          </div>

                          <h3 className='font-semibold text-white mb-2'>{ticket.subject}</h3>
                          <p className='text-sm text-gray-400 line-clamp-2'>{ticket.message}</p>

                          <div className='flex items-center gap-4 mt-3 text-xs text-gray-500'>
                            <span>{new Date(ticket.createdAt).toLocaleDateString()}</span>
                            {(ticket.messagesCount ||
                              (ticket.messages && ticket.messages.length) ||
                              0) > 0 && (
                              <span className='flex items-center gap-1'>
                                <MessageSquare className='w-3 h-3' />
                                {ticket.messagesCount ||
                                  (ticket.messages && ticket.messages.length) ||
                                  0}{' '}
                                {(ticket.messagesCount ||
                                  (ticket.messages && ticket.messages.length) ||
                                  0) === 1
                                  ? 'reply'
                                  : 'replies'}
                              </span>
                            )}
                            {ticket.assignedToUserName && (
                              <span>Assigned to: {ticket.assignedToUserName}</span>
                            )}
                          </div>
                        </div>

                        <Button
                          variant='primary'
                          size='sm'
                          onClick={() => openChat(ticket)}
                          className='ml-4 gap-2'
                        >
                          <MessageCircle className='w-4 h-4' />
                          Open Chat
                        </Button>
                      </div>

                      <AnimatePresence>
                        {isExpanded && ticket.messages && ticket.messages.length > 0 && (
                          <motion.div
                            initial={{ height: 0, opacity: 0 }}
                            animate={{ height: 'auto', opacity: 1 }}
                            exit={{ height: 0, opacity: 0 }}
                            transition={{ duration: 0.2 }}
                            className='mt-4 pt-4 border-t border-white/10'
                          >
                            <div className='space-y-3'>
                              {ticket.messages?.map(msg => (
                                <div
                                  key={msg.id}
                                  className={`p-3 rounded-lg ${
                                    msg.isStaffReply
                                      ? 'bg-blue-500/10 border border-blue-500/20'
                                      : 'bg-white/5 border border-white/10'
                                  }`}
                                >
                                  <div className='flex justify-between items-start mb-1'>
                                    <span className='text-xs font-semibold text-gray-300'>
                                      {msg.senderName} {msg.isStaffReply && '(Support)'}
                                    </span>
                                    <span className='text-xs text-gray-500'>
                                      {new Date(msg.createdAt).toLocaleString()}
                                    </span>
                                  </div>
                                  <p className='text-sm text-gray-400'>{msg.message}</p>
                                </div>
                              ))}
                            </div>
                          </motion.div>
                        )}
                      </AnimatePresence>
                    </CardContent>
                  </Card>
                </motion.div>
              );
            })}
          </div>

          {/* Pagination */}
          {pagination.totalPages > 1 && (
            <div className='flex items-center justify-between mt-6 pt-6 border-t border-white/10'>
              <div className='text-sm text-gray-400'>
                Showing {(pagination.page - 1) * pagination.pageSize + 1} to{' '}
                {Math.min(pagination.page * pagination.pageSize, pagination.totalCount)} of{' '}
                {pagination.totalCount} tickets
              </div>
              <div className='flex items-center gap-2'>
                <Button
                  variant='ghost'
                  size='sm'
                  onClick={() => handlePageChange(pagination.page - 1)}
                  disabled={!pagination.hasPreviousPage}
                >
                  ← Previous
                </Button>
                <span className='text-sm text-gray-400 px-3'>
                  Page {pagination.page} of {pagination.totalPages}
                </span>
                <Button
                  variant='ghost'
                  size='sm'
                  onClick={() => handlePageChange(pagination.page + 1)}
                  disabled={!pagination.hasNextPage}
                >
                  Next →
                </Button>
              </div>
            </div>
          )}
        </CardContent>
      </Card>

      {/* Ticket Chat Modal */}
      {selectedTicket && (
        <TicketChat
          ticket={selectedTicket}
          isOpen={isChatOpen}
          onClose={closeChat}
          onTicketUpdate={handleTicketUpdate}
        />
      )}
    </>
  );
}
