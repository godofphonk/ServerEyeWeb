'use client';

import { useState } from 'react';
import { motion, AnimatePresence } from 'framer-motion';
import { Mail, AlertCircle, FileText, Clock } from 'lucide-react';
import { Card, CardHeader, CardTitle, CardContent } from '@/components/ui/Card';
import { Input } from '@/components/ui/Input';
import { Button } from '@/components/ui/Button';
import { ticketApi } from '@/lib/ticketApi';
import { Ticket, TicketStatus } from '@/types';

const statusColors = {
  [TicketStatus.New]: 'text-blue-400',
  [TicketStatus.Open]: 'text-yellow-400',
  [TicketStatus.InProgress]: 'text-purple-400',
  [TicketStatus.Resolved]: 'text-green-400',
  [TicketStatus.Closed]: 'text-gray-400',
  [TicketStatus.Reopened]: 'text-orange-400',
};

export function TicketHistory() {
  const [email, setEmail] = useState('');
  const [tickets, setTickets] = useState<Ticket[]>([]);
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const handleFetch = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!email.trim()) return;

    setIsLoading(true);
    setError(null);
    setTickets([]);

    try {
      const result = await ticketApi.getTicketsByEmail(email.trim());
      setTickets(result);
      if (result.length === 0) {
        setError('No tickets found for this email');
      }
    } catch (err: any) { // eslint-disable-line @typescript-eslint/no-explicit-any
      setError('Failed to fetch tickets');
    } finally {
      setIsLoading(false);
    }
  };

  return (
    <Card>
      <CardHeader>
        <CardTitle className='text-lg'>Your Ticket History</CardTitle>
        <p className='text-sm text-gray-400 mt-2'>
          View all your support tickets by entering your email
        </p>
      </CardHeader>
      <CardContent>
        <form onSubmit={handleFetch} className='space-y-4'>
          <div className='flex gap-2'>
            <Input
              type='email'
              placeholder='your@email.com'
              value={email}
              onChange={e => setEmail(e.target.value)}
              disabled={isLoading}
              className='flex-1'
            />
            <Button type='submit' isLoading={isLoading} disabled={!email.trim()}>
              <Mail className='w-4 h-4' />
            </Button>
          </div>

          <AnimatePresence mode='wait'>
            {error && (
              <motion.div
                initial={{ opacity: 0, y: -10 }}
                animate={{ opacity: 1, y: 0 }}
                exit={{ opacity: 0, y: -10 }}
                className='p-3 bg-yellow-500/10 border border-yellow-500/20 rounded-lg flex items-center gap-2'
              >
                <AlertCircle className='w-4 h-4 text-yellow-400' />
                <p className='text-sm text-yellow-400'>{error}</p>
              </motion.div>
            )}

            {tickets.length > 0 && (
              <motion.div
                initial={{ opacity: 0 }}
                animate={{ opacity: 1 }}
                exit={{ opacity: 0 }}
                className='space-y-3 max-h-96 overflow-y-auto'
              >
                {tickets.map((ticket, index) => (
                  <motion.div
                    key={ticket.id}
                    initial={{ opacity: 0, y: 20 }}
                    animate={{ opacity: 1, y: 0 }}
                    transition={{ delay: index * 0.05 }}
                    className='p-4 bg-white/5 border border-white/10 rounded-xl hover:bg-white/10 transition-colors'
                  >
                    <div className='flex items-start justify-between mb-2'>
                      <div className='flex items-center gap-2'>
                        <FileText className='w-4 h-4 text-gray-400' />
                        <span className='font-semibold text-sm'>{ticket.ticketNumber}</span>
                      </div>
                      <span className={`text-xs font-semibold ${statusColors[ticket.status]}`}>
                        {ticket.statusDisplay}
                      </span>
                    </div>

                    <h4 className='font-semibold mb-1'>{ticket.subject}</h4>
                    <p className='text-sm text-gray-400 line-clamp-2 mb-2'>{ticket.message}</p>

                    <div className='flex items-center justify-between text-xs text-gray-500'>
                      <div className='flex items-center gap-1'>
                        <Clock className='w-3 h-3' />
                        <span>{new Date(ticket.createdAt).toLocaleDateString()}</span>
                      </div>
                      <span className='text-gray-400'>Priority: {ticket.priorityDisplay}</span>
                    </div>

                    {ticket.messages.length > 0 && (
                      <div className='mt-2 pt-2 border-t border-white/10'>
                        <p className='text-xs text-gray-500'>
                          {ticket.messages.length}{' '}
                          {ticket.messages.length === 1 ? 'reply' : 'replies'}
                        </p>
                      </div>
                    )}
                  </motion.div>
                ))}
              </motion.div>
            )}
          </AnimatePresence>
        </form>
      </CardContent>
    </Card>
  );
}
