'use client';

import { useState, useEffect, useRef } from 'react';
import { motion, AnimatePresence } from 'framer-motion';
import {
  MessageSquare,
  Send,
  X,
  Clock,
  AlertCircle,
  CheckCircle,
  User,
  Headset,
  Loader2,
  ArrowLeft,
} from 'lucide-react';
import { Button } from '@/components/ui/Button';
import { Input } from '@/components/ui/Input';
import { ticketApi } from '@/lib/ticketApi';
import { Ticket, TicketMessage, TicketStatus, AxiosApiError } from '@/types';
import { useAuth } from '@/context/AuthContext';
import { useToast } from '@/hooks/useToast';

interface TicketChatProps {
  ticket: Ticket;
  isOpen: boolean;
  onClose: () => void;
  onTicketUpdate?: () => void;
}

const statusConfig = {
  [TicketStatus.New]: {
    icon: Clock,
    color: 'text-blue-400',
    bg: 'bg-blue-500/10',
    border: 'border-blue-500/30',
    shadow: 'shadow-blue-500/30',
  },
  [TicketStatus.Open]: {
    icon: AlertCircle,
    color: 'text-yellow-400',
    bg: 'bg-yellow-500/10',
    border: 'border-yellow-500/30',
    shadow: 'shadow-yellow-500/30',
  },
  [TicketStatus.InProgress]: {
    icon: Loader2,
    color: 'text-purple-400',
    bg: 'bg-purple-500/10',
    border: 'border-purple-500/30',
    shadow: 'shadow-purple-500/30',
  },
  [TicketStatus.Resolved]: {
    icon: CheckCircle,
    color: 'text-green-400',
    bg: 'bg-green-500/10',
    border: 'border-green-500/30',
    shadow: 'shadow-green-500/30',
  },
  [TicketStatus.Closed]: {
    icon: X,
    color: 'text-gray-400',
    bg: 'bg-gray-500/10',
    border: 'border-gray-500/30',
    shadow: 'shadow-gray-500/30',
  },
  [TicketStatus.Reopened]: {
    icon: AlertCircle,
    color: 'text-orange-400',
    bg: 'bg-orange-500/10',
    border: 'border-orange-500/30',
    shadow: 'shadow-orange-500/30',
  },
};

export function TicketChat({ ticket, isOpen, onClose, onTicketUpdate }: TicketChatProps) {
  const { user } = useAuth();
  const toast = useToast();
  const [messages, setMessages] = useState<TicketMessage[]>([]);
  const [newMessage, setNewMessage] = useState('');
  const [isLoading, setIsLoading] = useState(false);
  const [isSending, setIsSending] = useState(false);
  const messagesEndRef = useRef<HTMLDivElement>(null);
  const inputRef = useRef<HTMLInputElement>(null);

  const StatusIcon = statusConfig[ticket.status].icon;

  useEffect(() => {
    if (isOpen && ticket) {
      loadMessages();
    }
  }, [isOpen, ticket.id]);

  useEffect(() => {
    scrollToBottom();
  }, [messages]);

  const scrollToBottom = () => {
    messagesEndRef.current?.scrollIntoView({ behavior: 'smooth' });
  };

  const loadMessages = async () => {
    setIsLoading(true);
    try {
      // Load fresh ticket data with messages from backend
      const updatedTicket = await ticketApi.getTicketById(ticket.id);
      setMessages(updatedTicket.messages || []);
    } catch (_error) {
      // Fallback to ticket object messages
      setMessages(ticket.messages || []);
    } finally {
      setIsLoading(false);
    }
  };

  const handleSendMessage = async () => {
    if (!newMessage.trim() || isSending) return;

    setIsSending(true);
    const messageText = newMessage.trim();
    setNewMessage('');

    try {
      const messageData = {
        message: messageText,
        senderName: user?.username || 'User',
        senderEmail: user?.email || '',
        isStaffReply: false,
      };

      // Add optimistic update
      const optimisticMessage: TicketMessage = {
        id: `temp-${Date.now()}`,
        ticketId: ticket.id,
        message: messageText,
        senderName: user?.username || 'User',
        senderEmail: user?.email || '',
        isStaffReply: false,
        createdAt: new Date().toISOString(),
      };

      setMessages(prev => [...prev, optimisticMessage]);
      scrollToBottom();

      // Send to backend
      await ticketApi.addTicketMessage(ticket.id, messageData);

      toast.info('Message Sent', 'Your message has been sent to the support team');

      // Reload messages from backend to get the latest state
      await loadMessages();

      // Notify parent to refresh ticket list
      onTicketUpdate?.();
    } catch (error: unknown) {
      const errorMessage =
        (error as AxiosApiError)?.response?.data?.message ||
        (error as AxiosApiError)?.message ||
        'Unknown error occurred';

      toast.error('Send Failed', `Failed to send message: ${errorMessage}`);

      // Remove optimistic message on error
      setMessages(prev => prev.filter(msg => !msg.id.startsWith('temp-')));
    } finally {
      setIsSending(false);
      inputRef.current?.focus();
    }
  };

  const handleKeyPress = (e: React.KeyboardEvent) => {
    if (e.key === 'Enter' && !e.shiftKey) {
      e.preventDefault();
      handleSendMessage();
    }
  };

  const formatTime = (dateString: string) => {
    return new Date(dateString).toLocaleTimeString('en-US', {
      hour: '2-digit',
      minute: '2-digit',
    });
  };

  const formatDate = (dateString: string) => {
    const date = new Date(dateString);
    const today = new Date();
    const yesterday = new Date(today);
    yesterday.setDate(yesterday.getDate() - 1);

    if (date.toDateString() === today.toDateString()) {
      return 'Today';
    } else if (date.toDateString() === yesterday.toDateString()) {
      return 'Yesterday';
    } else {
      return date.toLocaleDateString();
    }
  };

  const groupMessagesByDate = (messages: TicketMessage[]) => {
    const groups: { [date: string]: TicketMessage[] } = {};

    messages.forEach(message => {
      const date = new Date(message.createdAt).toDateString();
      if (!groups[date]) {
        groups[date] = [];
      }
      groups[date].push(message);
    });

    return groups;
  };

  return (
    <AnimatePresence>
      {isOpen && (
        <motion.div
          initial={{ opacity: 0 }}
          animate={{ opacity: 1 }}
          exit={{ opacity: 0 }}
          className='fixed inset-0 bg-black/50 backdrop-blur-sm z-50 flex items-center justify-center p-4'
          onClick={onClose}
        >
          <motion.div
            initial={{ scale: 0.95, opacity: 0, y: 20 }}
            animate={{ scale: 1, opacity: 1, y: 0 }}
            exit={{ scale: 0.95, opacity: 0, y: 20 }}
            transition={{ type: 'spring', stiffness: 300, damping: 30 }}
            className='bg-gray-900/95 backdrop-blur-xl border border-white/10 rounded-2xl w-full max-w-4xl max-h-[90vh] flex flex-col overflow-hidden shadow-2xl shadow-black/50 relative'
            onClick={e => e.stopPropagation()}
          >
            <motion.div
              className='absolute inset-0 bg-gradient-to-br from-blue-500/5 to-purple-500/5'
              animate={{ opacity: [0, 1, 0] }}
              transition={{ duration: 3, repeat: Infinity, ease: 'easeInOut' }}
            />
            {/* Header */}
            <div className='flex items-center justify-between p-6 border-b border-white/10 relative z-10'>
              <div className='flex items-center gap-4'>
                <Button variant='ghost' size='sm' onClick={onClose} className='lg:hidden'>
                  <ArrowLeft className='w-4 h-4' />
                </Button>
                <div>
                  <h2 className='text-xl font-semibold text-white'>{ticket.subject}</h2>
                  <div className='flex items-center gap-3 mt-1'>
                    <span className='text-sm text-gray-400'>#{ticket.ticketNumber}</span>
                    <div
                      className={`flex items-center gap-1 px-2 py-1 rounded-full text-xs ${statusConfig[ticket.status].bg} ${statusConfig[ticket.status].border} ${statusConfig[ticket.status].color} shadow ${statusConfig[ticket.status].shadow}`}
                    >
                      <StatusIcon className='w-3 h-3' />
                      {ticket.statusDisplay}
                    </div>
                  </div>
                </div>
              </div>
              <motion.button whileHover={{ scale: 1.1, rotate: 90 }} whileTap={{ scale: 0.9 }}>
                <Button variant='ghost' size='sm' onClick={onClose} className='hidden lg:flex'>
                  <X className='w-5 h-5' />
                </Button>
              </motion.button>
            </div>

            {/* Messages Area */}
            <div className='flex-1 overflow-hidden flex flex-col relative z-10'>
              <div className='flex-1 overflow-y-auto p-6 space-y-4'>
                {isLoading ? (
                  <div className='flex items-center justify-center py-12'>
                    <Loader2 className='w-6 h-6 animate-spin text-blue-400' />
                  </div>
                ) : messages.length === 0 ? (
                  <div className='text-center py-12'>
                    <MessageSquare className='w-12 h-12 text-gray-400 mx-auto mb-4' />
                    <p className='text-gray-400'>No messages yet</p>
                    <p className='text-sm text-gray-500 mt-2'>Start the conversation</p>
                  </div>
                ) : (
                  Object.entries(groupMessagesByDate(messages)).map(([date, dateMessages]) => (
                    <div key={date} className='space-y-3'>
                      <div className='flex items-center justify-center'>
                        <span className='text-xs text-gray-500 bg-gray-800 px-3 py-1 rounded-full'>
                          {formatDate(date)}
                        </span>
                      </div>
                      {dateMessages.map((message, index) => (
                        <motion.div
                          key={message.id}
                          initial={{ opacity: 0, y: 10, scale: 0.95 }}
                          animate={{ opacity: 1, y: 0, scale: 1 }}
                          transition={{ delay: index * 0.05 }}
                          whileHover={{ scale: 1.02 }}
                          className={`flex ${message.isStaffReply ? 'justify-start' : 'justify-end'}`}
                        >
                          <div
                            className={`flex items-start gap-3 max-w-[70%] ${message.isStaffReply ? 'flex-row' : 'flex-row-reverse'}`}
                          >
                            <motion.div
                              whileHover={{ rotate: 360 }}
                              transition={{ duration: 0.5 }}
                              className={`w-8 h-8 rounded-full flex items-center justify-center flex-shrink-0 border ${
                                message.isStaffReply
                                  ? 'bg-blue-500/20 text-blue-400 border-blue-500/30'
                                  : 'bg-purple-500/20 text-purple-400 border-purple-500/30'
                              }`}
                            >
                              {message.isStaffReply ? (
                                <Headset className='w-4 h-4' />
                              ) : (
                                <User className='w-4 h-4' />
                              )}
                            </motion.div>
                            <div
                              className={`space-y-1 ${message.isStaffReply ? 'items-start' : 'items-end'} flex flex-col`}
                            >
                              <div
                                className={`px-4 py-2 rounded-2xl ${
                                  message.isStaffReply
                                    ? 'bg-gray-800 text-white border border-white/5 shadow-lg'
                                    : 'bg-gradient-to-r from-blue-600 to-blue-700 text-white shadow-lg shadow-blue-500/30'
                                }`}
                              >
                                <p className='text-sm whitespace-pre-wrap'>{message.message}</p>
                              </div>
                              <div
                                className={`flex items-center gap-2 text-xs text-gray-500 ${message.isStaffReply ? '' : 'justify-end'}`}
                              >
                                <span>{message.senderName}</span>
                                <span>•</span>
                                <span>{formatTime(message.createdAt)}</span>
                              </div>
                            </div>
                          </div>
                        </motion.div>
                      ))}
                    </div>
                  ))
                )}
                <div ref={messagesEndRef} />
              </div>

              {/* Message Input */}
              <div className='border-t border-white/10 p-4 bg-gradient-to-r from-blue-500/5 to-purple-500/5'>
                <div className='flex items-end gap-3'>
                  <div className='flex-1'>
                    <Input
                      ref={inputRef}
                      value={newMessage}
                      onChange={e => setNewMessage(e.target.value)}
                      onKeyPress={handleKeyPress}
                      placeholder='Type your message...'
                      disabled={isSending}
                      className='bg-gray-800 border-white/10 text-white placeholder-gray-500'
                    />
                  </div>
                  <Button
                    onClick={handleSendMessage}
                    disabled={!newMessage.trim() || isSending}
                    size='sm'
                    className='px-4 shadow-lg shadow-blue-500/30'
                  >
                    {isSending ? (
                      <Loader2 className='w-4 h-4 animate-spin' />
                    ) : (
                      <Send className='w-4 h-4' />
                    )}
                  </Button>
                </div>
              </div>
            </div>
          </motion.div>
        </motion.div>
      )}
    </AnimatePresence>
  );
}
