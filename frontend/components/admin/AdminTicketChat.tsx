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
  XCircle,
  Trash2,
  PlayCircle,
  RotateCcw,
} from 'lucide-react';
import { Button } from '@/components/ui/Button';
import { Input } from '@/components/ui/Input';
import { ticketApi } from '@/lib/ticketApi';
import { Ticket, TicketMessage, TicketStatus, AxiosApiError } from '@/types';
import { useAuth } from '@/context/AuthContext';
import { useToast } from '@/hooks/useToast';

interface AdminTicketChatProps {
  ticket: Ticket;
  isOpen: boolean;
  onClose: () => void;
  onTicketUpdate?: () => void;
}

const statusConfig = {
  [TicketStatus.New]: { icon: Clock, color: 'text-blue-400', bg: 'bg-blue-500/10', label: 'New' },
  [TicketStatus.Open]: {
    icon: AlertCircle,
    color: 'text-yellow-400',
    bg: 'bg-yellow-500/10',
    label: 'Open',
  },
  [TicketStatus.InProgress]: {
    icon: Loader2,
    color: 'text-purple-400',
    bg: 'bg-purple-500/10',
    label: 'In Progress',
  },
  [TicketStatus.Resolved]: {
    icon: CheckCircle,
    color: 'text-green-400',
    bg: 'bg-green-500/10',
    label: 'Resolved',
  },
  [TicketStatus.Closed]: {
    icon: XCircle,
    color: 'text-gray-400',
    bg: 'bg-gray-500/10',
    label: 'Closed',
  },
  [TicketStatus.Reopened]: {
    icon: AlertCircle,
    color: 'text-orange-400',
    bg: 'bg-orange-500/10',
    label: 'Reopened',
  },
};

export function AdminTicketChat({ ticket, isOpen, onClose, onTicketUpdate }: AdminTicketChatProps) {
  const { user } = useAuth();
  const toast = useToast();
  const [currentTicket, setCurrentTicket] = useState<Ticket>(ticket);
  const [messages, setMessages] = useState<TicketMessage[]>([]);
  const [newMessage, setNewMessage] = useState('');
  const [isLoading, setIsLoading] = useState(false);
  const [isSending, setIsSending] = useState(false);
  const [isUpdatingStatus, setIsUpdatingStatus] = useState(false);
  const messagesEndRef = useRef<HTMLDivElement>(null);
  const inputRef = useRef<HTMLInputElement>(null);

  const StatusIcon = statusConfig[currentTicket.status].icon;

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
      const updatedTicket = await ticketApi.getTicketById(ticket.id);
      setCurrentTicket(updatedTicket);
      setMessages(updatedTicket.messages || []);
    } catch (_error) {
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
        senderName: 'Support Team',
        senderEmail: user?.email || 'support@servereye.dev',
        isStaffReply: true, // Admin reply
      };

      // Add optimistic update
      const optimisticMessage: TicketMessage = {
        id: `temp-${Date.now()}`,
        ticketId: ticket.id,
        message: messageText,
        senderName: 'Support Team',
        senderEmail: user?.email || 'support@servereye.dev',
        isStaffReply: true,
        createdAt: new Date().toISOString(),
      };

      setMessages(prev => [...prev, optimisticMessage]);
      scrollToBottom();

      // Send to backend
      await ticketApi.addTicketMessage(ticket.id, messageData);

      // Reload messages from backend
      await loadMessages();

      // Notify parent to refresh ticket list
      onTicketUpdate?.();
    } catch (_error) {
      // Remove optimistic message on error
      setMessages(prev => prev.filter(msg => !msg.id.startsWith('temp-')));
    } finally {
      setIsSending(false);
      inputRef.current?.focus();
    }
  };

  const handleStatusChange = async (newStatus: TicketStatus) => {
    setIsUpdatingStatus(true);
    try {
      await ticketApi.updateTicketStatus(ticket.id, { status: newStatus });

      // Update local ticket
      setCurrentTicket(prev => ({ ...prev, status: newStatus }));

      // Notify parent
      onTicketUpdate?.();
    } catch (_error) {
      // ignore error
    } finally {
      setIsUpdatingStatus(false);
    }
  };

  const handleDeleteTicket = async () => {
    if (
      !confirm(
        `Are you sure you want to delete ticket #${currentTicket.ticketNumber}? This action cannot be undone.`,
      )
    ) {
      return;
    }

    try {
      setIsLoading(true);
      await ticketApi.deleteTicket(currentTicket.id);
      toast.success(
        'Ticket Deleted',
        `Ticket #${currentTicket.ticketNumber} has been successfully deleted`,
      );
      onTicketUpdate?.();
      onClose();
    } catch (error: unknown) {
      const errorMessage =
        (error as AxiosApiError)?.response?.data?.message ||
        (error as AxiosApiError)?.message ||
        'Unknown error occurred';

      if (errorMessage.includes('Only administrators')) {
        toast.error('Permission Denied', 'You do not have permission to delete tickets');
      } else if (errorMessage.includes('not found')) {
        toast.error('Ticket Not Found', 'The ticket you are trying to delete does not exist');
      } else {
        toast.error('Delete Failed', `Failed to delete ticket: ${errorMessage}`);
      }
    } finally {
      setIsLoading(false);
    }
  };

  const handleKeyPress = (e: React.KeyboardEvent<HTMLInputElement>) => {
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
            initial={{ scale: 0.95, opacity: 0 }}
            animate={{ scale: 1, opacity: 1 }}
            exit={{ scale: 0.95, opacity: 0 }}
            transition={{ type: 'spring', duration: 0.3 }}
            className='bg-gray-900 border border-white/10 rounded-2xl w-full max-w-4xl max-h-[90vh] flex flex-col overflow-hidden'
            onClick={e => e.stopPropagation()}
          >
            {/* Header */}
            <div className='flex flex-col gap-4 p-6 border-b border-white/10'>
              <div className='flex items-center justify-between'>
                <div className='flex items-center gap-4'>
                  <Button variant='ghost' size='sm' onClick={onClose} className='lg:hidden'>
                    <ArrowLeft className='w-4 h-4' />
                  </Button>
                  <div>
                    <h2 className='text-xl font-semibold text-white'>{currentTicket.subject}</h2>
                    <div className='flex items-center gap-3 mt-1'>
                      <span className='text-sm text-gray-400'>#{currentTicket.ticketNumber}</span>
                      <div
                        className={`flex items-center gap-1 px-2 py-1 rounded-full text-xs ${statusConfig[currentTicket.status].bg} ${statusConfig[currentTicket.status].color}`}
                      >
                        <StatusIcon className='w-3 h-3' />
                        {statusConfig[currentTicket.status].label}
                      </div>
                      <span className='text-xs text-gray-500'>{currentTicket.priorityDisplay}</span>
                    </div>
                    <div className='text-sm text-gray-400 mt-1'>
                      From: {currentTicket.name} ({currentTicket.email})
                    </div>
                  </div>
                </div>
                <Button variant='ghost' size='sm' onClick={onClose} className='hidden lg:flex'>
                  <X className='w-5 h-5' />
                </Button>
              </div>

              {/* Quick Actions */}
              <div className='flex items-center gap-2 flex-wrap'>
                <span className='text-sm text-gray-400 mr-2'>Quick Actions:</span>

                {/* Start Progress */}
                {currentTicket.status === TicketStatus.New && (
                  <Button
                    variant='ghost'
                    size='sm'
                    onClick={() => handleStatusChange(TicketStatus.InProgress)}
                    disabled={isUpdatingStatus}
                    className='gap-2'
                  >
                    <PlayCircle className='w-4 h-4' />
                    Start Work
                  </Button>
                )}

                {/* Resolve */}
                {(currentTicket.status === TicketStatus.InProgress ||
                  currentTicket.status === TicketStatus.Open) && (
                  <Button
                    variant='ghost'
                    size='sm'
                    onClick={() => handleStatusChange(TicketStatus.Resolved)}
                    disabled={isUpdatingStatus}
                    className='gap-2 text-green-400 hover:text-green-300'
                  >
                    <CheckCircle className='w-4 h-4' />
                    Mark Resolved
                  </Button>
                )}

                {/* Close */}
                {currentTicket.status === TicketStatus.Resolved && (
                  <Button
                    variant='ghost'
                    size='sm'
                    onClick={() => handleStatusChange(TicketStatus.Closed)}
                    disabled={isUpdatingStatus}
                    className='gap-2'
                  >
                    <XCircle className='w-4 h-4' />
                    Close Ticket
                  </Button>
                )}

                {/* Reopen */}
                {(currentTicket.status === TicketStatus.Closed ||
                  currentTicket.status === TicketStatus.Resolved) && (
                  <Button
                    variant='ghost'
                    size='sm'
                    onClick={() => handleStatusChange(TicketStatus.Reopened)}
                    disabled={isUpdatingStatus}
                    className='gap-2 text-orange-400 hover:text-orange-300'
                  >
                    <RotateCcw className='w-4 h-4' />
                    Reopen
                  </Button>
                )}

                {/* Delete */}
                <Button
                  variant='ghost'
                  size='sm'
                  onClick={handleDeleteTicket}
                  disabled={isLoading}
                  className='gap-2 text-red-400 hover:text-red-300 ml-auto'
                >
                  <Trash2 className='w-4 h-4' />
                  Delete Ticket
                </Button>
              </div>

              {/* All Status Options (collapsed) */}
              <details className='group'>
                <summary className='text-sm text-gray-400 cursor-pointer hover:text-gray-300 list-none flex items-center gap-2'>
                  <span>All Status Options</span>
                  <svg
                    className='w-4 h-4 transition-transform group-open:rotate-180'
                    fill='none'
                    stroke='currentColor'
                    viewBox='0 0 24 24'
                  >
                    <path
                      strokeLinecap='round'
                      strokeLinejoin='round'
                      strokeWidth={2}
                      d='M19 9l-7 7-7-7'
                    />
                  </svg>
                </summary>
                <div className='flex gap-2 flex-wrap mt-2'>
                  {Object.entries(statusConfig).map(([status, config]) => {
                    const Icon = config.icon;
                    const statusNum = Number(status) as TicketStatus;
                    const isActive = currentTicket.status === statusNum;

                    return (
                      <Button
                        key={status}
                        variant={isActive ? 'primary' : 'ghost'}
                        size='sm'
                        onClick={() => handleStatusChange(statusNum)}
                        disabled={isUpdatingStatus || isActive}
                        className='gap-1 text-xs'
                      >
                        <Icon className='w-3 h-3' />
                        {config.label}
                      </Button>
                    );
                  })}
                </div>
              </details>
            </div>

            {/* Messages Area */}
            <div className='flex-1 overflow-hidden flex flex-col'>
              <div className='flex-1 overflow-y-auto p-6 space-y-4'>
                {isLoading ? (
                  <div className='flex items-center justify-center py-12'>
                    <Loader2 className='w-6 h-6 animate-spin text-blue-400' />
                  </div>
                ) : messages.length === 0 ? (
                  <div className='text-center py-12'>
                    <MessageSquare className='w-12 h-12 text-gray-400 mx-auto mb-4' />
                    <p className='text-gray-400'>No messages yet</p>
                    <p className='text-sm text-gray-500 mt-2'>Be the first to respond</p>
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
                          initial={{ opacity: 0, y: 10 }}
                          animate={{ opacity: 1, y: 0 }}
                          transition={{ delay: index * 0.05 }}
                          className={`flex ${message.isStaffReply ? 'justify-end' : 'justify-start'}`}
                        >
                          <div
                            className={`flex items-start gap-3 max-w-[70%] ${message.isStaffReply ? 'flex-row-reverse' : 'flex-row'}`}
                          >
                            <div
                              className={`w-8 h-8 rounded-full flex items-center justify-center flex-shrink-0 ${
                                message.isStaffReply
                                  ? 'bg-blue-500/20 text-blue-400'
                                  : 'bg-purple-500/20 text-purple-400'
                              }`}
                            >
                              {message.isStaffReply ? (
                                <Headset className='w-4 h-4' />
                              ) : (
                                <User className='w-4 h-4' />
                              )}
                            </div>
                            <div
                              className={`space-y-1 ${message.isStaffReply ? 'items-end' : 'items-start'} flex flex-col`}
                            >
                              <div
                                className={`px-4 py-2 rounded-2xl ${
                                  message.isStaffReply
                                    ? 'bg-blue-600 text-white'
                                    : 'bg-gray-800 text-white border border-white/5'
                                }`}
                              >
                                <p className='text-sm whitespace-pre-wrap'>{message.message}</p>
                              </div>
                              <div
                                className={`flex items-center gap-2 text-xs text-gray-500 ${message.isStaffReply ? 'justify-end' : ''}`}
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
              <div className='border-t border-white/10 p-4 bg-gray-900/50'>
                <div className='flex items-end gap-3'>
                  <div className='flex-1'>
                    <Input
                      ref={inputRef}
                      value={newMessage}
                      onChange={e => setNewMessage(e.target.value)}
                      onKeyPress={handleKeyPress}
                      placeholder='Type your response as Support Team...'
                      disabled={isSending}
                      className='bg-gray-800 border-white/10 text-white placeholder-gray-500'
                    />
                  </div>
                  <Button
                    onClick={handleSendMessage}
                    disabled={!newMessage.trim() || isSending}
                    size='sm'
                    className='px-4'
                  >
                    {isSending ? (
                      <Loader2 className='w-4 h-4 animate-spin' />
                    ) : (
                      <Send className='w-4 h-4' />
                    )}
                  </Button>
                </div>
                <p className='text-xs text-gray-500 mt-2'>
                  Responding as Support Team • Messages are sent to {currentTicket.email}
                </p>
              </div>
            </div>
          </motion.div>
        </motion.div>
      )}
    </AnimatePresence>
  );
}
