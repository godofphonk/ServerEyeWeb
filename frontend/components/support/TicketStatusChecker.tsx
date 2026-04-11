'use client';

import { useState } from 'react';
import { motion, AnimatePresence } from 'framer-motion';
import { Search, Loader2, CheckCircle, Clock, AlertCircle, XCircle } from 'lucide-react';
import { Card, CardHeader, CardTitle, CardContent } from '@/components/ui/Card';
import { Input } from '@/components/ui/Input';
import { Button } from '@/components/ui/Button';
import { ticketApi } from '@/lib/ticketApi';
import { Ticket, TicketStatus } from '@/types';

const statusConfig = {
  [TicketStatus.New]: {
    icon: Clock,
    color: 'text-blue-400',
    bg: 'bg-blue-500/10',
    border: 'border-blue-500/20',
    label: 'New',
  },
  [TicketStatus.Open]: {
    icon: AlertCircle,
    color: 'text-yellow-400',
    bg: 'bg-yellow-500/10',
    border: 'border-yellow-500/20',
    label: 'Open',
  },
  [TicketStatus.InProgress]: {
    icon: Loader2,
    color: 'text-purple-400',
    bg: 'bg-purple-500/10',
    border: 'border-purple-500/20',
    label: 'In Progress',
  },
  [TicketStatus.Resolved]: {
    icon: CheckCircle,
    color: 'text-green-400',
    bg: 'bg-green-500/10',
    border: 'border-green-500/20',
    label: 'Resolved',
  },
  [TicketStatus.Closed]: {
    icon: XCircle,
    color: 'text-gray-400',
    bg: 'bg-gray-500/10',
    border: 'border-gray-500/20',
    label: 'Closed',
  },
  [TicketStatus.Reopened]: {
    icon: AlertCircle,
    color: 'text-orange-400',
    bg: 'bg-orange-500/10',
    border: 'border-orange-500/20',
    label: 'Reopened',
  },
};

export function TicketStatusChecker() {
  const [ticketNumber, setTicketNumber] = useState('');
  const [ticket, setTicket] = useState<Ticket | null>(null);
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const handleCheck = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!ticketNumber.trim()) return;

    setIsLoading(true);
    setError(null);
    setTicket(null);

    try {
      const result = await ticketApi.getTicketByNumber(ticketNumber.trim());
      setTicket(result);
    } catch (err: any) { // eslint-disable-line @typescript-eslint/no-explicit-any
      setError(err.response?.status === 404 ? 'Ticket not found' : 'Failed to fetch ticket');
    } finally {
      setIsLoading(false);
    }
  };

  const config = ticket ? statusConfig[ticket.status] : null;
  const StatusIcon = config?.icon;

  return (
    <Card>
      <CardHeader>
        <CardTitle className='text-lg'>Check Ticket Status</CardTitle>
        <p className='text-sm text-gray-400 mt-2'>
          Enter your ticket number to check its current status
        </p>
      </CardHeader>
      <CardContent>
        <form onSubmit={handleCheck} className='space-y-4'>
          <div className='flex gap-2'>
            <Input
              placeholder='TKT-20240224124530-1234'
              value={ticketNumber}
              onChange={e => setTicketNumber(e.target.value)}
              disabled={isLoading}
              className='flex-1'
            />
            <Button type='submit' isLoading={isLoading} disabled={!ticketNumber.trim()}>
              <Search className='w-4 h-4' />
            </Button>
          </div>

          <AnimatePresence mode='wait'>
            {error && (
              <motion.div
                initial={{ opacity: 0, y: -10 }}
                animate={{ opacity: 1, y: 0 }}
                exit={{ opacity: 0, y: -10 }}
                className='p-3 bg-red-500/10 border border-red-500/20 rounded-lg flex items-center gap-2'
              >
                <AlertCircle className='w-4 h-4 text-red-400' />
                <p className='text-sm text-red-400'>{error}</p>
              </motion.div>
            )}

            {ticket && config && StatusIcon && (
              <motion.div
                initial={{ opacity: 0, y: 10 }}
                animate={{ opacity: 1, y: 0 }}
                exit={{ opacity: 0, y: -10 }}
                className='space-y-3'
              >
                <div className={`p-4 ${config.bg} border ${config.border} rounded-xl`}>
                  <div className='flex items-center gap-3 mb-3'>
                    <StatusIcon className={`w-5 h-5 ${config.color}`} />
                    <div>
                      <p className='font-semibold'>{ticket.ticketNumber}</p>
                      <p className={`text-sm ${config.color}`}>{config.label}</p>
                    </div>
                  </div>
                  <div className='space-y-2 text-sm'>
                    <div className='flex justify-between'>
                      <span className='text-gray-400'>Subject:</span>
                      <span className='text-white'>{ticket.subject}</span>
                    </div>
                    <div className='flex justify-between'>
                      <span className='text-gray-400'>Priority:</span>
                      <span className='text-white'>{ticket.priorityDisplay}</span>
                    </div>
                    <div className='flex justify-between'>
                      <span className='text-gray-400'>Created:</span>
                      <span className='text-white'>
                        {new Date(ticket.createdAt).toLocaleDateString()}
                      </span>
                    </div>
                    {ticket.assignedToUserName && (
                      <div className='flex justify-between'>
                        <span className='text-gray-400'>Assigned to:</span>
                        <span className='text-white'>{ticket.assignedToUserName}</span>
                      </div>
                    )}
                  </div>
                </div>

                {ticket.messages.length > 0 && (
                  <div className='space-y-2'>
                    <p className='text-sm font-semibold text-gray-300'>Recent Updates</p>
                    <div className='space-y-2 max-h-48 overflow-y-auto'>
                      {ticket.messages
                        .slice(-3)
                        .reverse()
                        .map(msg => (
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
                                {msg.senderName}
                              </span>
                              <span className='text-xs text-gray-500'>
                                {new Date(msg.createdAt).toLocaleDateString()}
                              </span>
                            </div>
                            <p className='text-sm text-gray-400'>{msg.message}</p>
                          </div>
                        ))}
                    </div>
                  </div>
                )}
              </motion.div>
            )}
          </AnimatePresence>
        </form>
      </CardContent>
    </Card>
  );
}
