'use client';

import { useState, useEffect } from 'react';
import { motion, AnimatePresence } from 'framer-motion';
import {
  MessageCircle,
  Mail,
  Send,
  AlertCircle,
  CheckCircle,
  Lock,
  Ticket,
  Plus,
  List,
} from 'lucide-react';
import { Button } from '@/components/ui/Button';
import { Input } from '@/components/ui/Input';
import { Card, CardHeader, CardTitle, CardContent } from '@/components/ui/Card';
import { MyTickets } from '@/components/support/MyTickets';
import { ticketApi } from '@/lib/ticketApi';
import { CreateTicketRequest } from '@/types';
import { useAuth } from '@/context/AuthContext';
import { useToast } from '@/hooks/useToast';

export default function SupportPage() {
  const { user, isAuthenticated } = useAuth();
  const toast = useToast();
  const [activeTab, setActiveTab] = useState<'create' | 'tickets'>('create');
  const [formData, setFormData] = useState({
    name: '',
    email: '',
    subject: '',
    message: '',
  });
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [submitStatus, setSubmitStatus] = useState<'success' | 'error' | null>(null);
  const [ticketNumber, setTicketNumber] = useState<string | null>(null);
  const [errorMessage, setErrorMessage] = useState<string | null>(null);

  // Auto-fill form with user data when authenticated
  useEffect(() => {
    if (user) {
      setFormData(prev => ({
        ...prev,
        name: user.username,
        email: user.email,
      }));
    }
  }, [user]);

  const validateForm = (): boolean => {
    if (!formData.name.trim()) {
      setErrorMessage('Name is required');
      return false;
    }
    if (!formData.email.trim() || !/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(formData.email)) {
      setErrorMessage('Valid email is required');
      return false;
    }
    if (!formData.subject.trim()) {
      setErrorMessage('Subject is required');
      return false;
    }
    if (!formData.message.trim()) {
      setErrorMessage('Message is required');
      return false;
    }
    return true;
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setIsSubmitting(true);
    setSubmitStatus(null);
    setErrorMessage(null);
    setTicketNumber(null);

    if (!isAuthenticated) {
      setErrorMessage('Please log in to create a ticket');
      setSubmitStatus('error');
      setIsSubmitting(false);
      return;
    }

    if (!validateForm()) {
      setIsSubmitting(false);
      setSubmitStatus('error');
      return;
    }

    try {
      const ticketData: CreateTicketRequest = {
        name: formData.name.trim(),
        email: formData.email.trim(),
        subject: formData.subject.trim(),
        message: formData.message.trim(),
      };

      const response = await ticketApi.createTicket(ticketData);
      console.log('[SupportPage] Ticket created:', response);
      setSubmitStatus('success');
      setTicketNumber(response.ticketNumber);
      setFormData({ name: '', email: '', subject: '', message: '' });

      toast.success(
        'Ticket Created',
        `Ticket #${response.ticketNumber} has been created successfully`,
      );

      // Refresh tickets list if user is on tickets tab
      if (activeTab === 'tickets') {
        // Trigger reload by updating the tab
        setActiveTab('create');
        setTimeout(() => setActiveTab('tickets'), 100);
      }
    } catch (error: any) {
      setSubmitStatus('error');
      const errorMessage =
        error.response?.data?.message ||
        'Failed to create ticket. Please try again or contact support@servereye.dev';
      setErrorMessage(errorMessage);

      toast.error('Creation Failed', `Failed to create ticket: ${errorMessage}`);
    } finally {
      setIsSubmitting(false);
    }
  };

  return (
    <main className='min-h-screen bg-black text-white'>
      <div className='absolute inset-0 bg-gradient-to-br from-blue-600/10 via-purple-600/10 to-pink-600/10' />

      <div className='relative z-10'>
        <div className='border-b border-white/10 bg-black/50 backdrop-blur-xl'>
          <div className='container mx-auto px-6 py-12'>
            <motion.div
              initial={{ opacity: 0, y: 20 }}
              animate={{ opacity: 1, y: 0 }}
              className='text-center max-w-3xl mx-auto'
            >
              <div className='inline-flex items-center gap-2 px-4 py-2 bg-blue-500/10 border border-blue-500/20 rounded-full mb-6'>
                <MessageCircle className='w-4 h-4 text-blue-400' />
                <span className='text-sm text-blue-400'>Support</span>
              </div>
              <h1 className='text-5xl md:text-6xl font-bold mb-6'>How can we help?</h1>
              <p className='text-xl text-gray-400'>Get in touch with our support team</p>
            </motion.div>
          </div>
        </div>

        {/* Mini Navigation */}
        <div className='border-b border-white/10 bg-black/30 backdrop-blur-sm sticky top-0 z-20'>
          <div className='container mx-auto px-6'>
            <div className='flex items-center justify-center py-4'>
              <div className='inline-flex items-center gap-1 p-1 bg-white/5 rounded-xl'>
                <Button
                  variant={activeTab === 'create' ? 'primary' : 'ghost'}
                  size='sm'
                  onClick={() => setActiveTab('create')}
                  className='gap-2'
                >
                  <Plus className='w-4 h-4' />
                  Create Ticket
                </Button>
                <Button
                  variant={activeTab === 'tickets' ? 'primary' : 'ghost'}
                  size='sm'
                  onClick={() => setActiveTab('tickets')}
                  className='gap-2'
                >
                  <List className='w-4 h-4' />
                  My Tickets
                </Button>
              </div>
            </div>
          </div>
        </div>

        <div className='container mx-auto px-6 py-16'>
          <div className='max-w-4xl mx-auto'>
            <AnimatePresence mode='wait'>
              {activeTab === 'create' ? (
                <motion.div
                  key='create'
                  initial={{ opacity: 0, x: 20 }}
                  animate={{ opacity: 1, x: 0 }}
                  exit={{ opacity: 0, x: -20 }}
                >
                  <Card>
                    <CardHeader>
                      <CardTitle>Create a Support Ticket</CardTitle>
                      <p className='text-sm text-gray-400 mt-2'>
                        Fill out the form below and we'll get back to you as soon as possible
                      </p>
                    </CardHeader>
                    <CardContent>
                      {submitStatus === 'success' && ticketNumber && (
                        <motion.div
                          initial={{ opacity: 0, y: -10 }}
                          animate={{ opacity: 1, y: 0 }}
                          className='mb-6 p-4 bg-green-500/10 border border-green-500/20 rounded-xl'
                        >
                          <div className='flex items-center gap-3 mb-2'>
                            <CheckCircle className='w-5 h-5 text-green-400' />
                            <p className='text-sm font-semibold text-green-400'>
                              Ticket created successfully!
                            </p>
                          </div>
                          <div className='ml-8 space-y-1'>
                            <div className='flex items-center gap-2'>
                              <Ticket className='w-4 h-4 text-green-400' />
                              <p className='text-sm text-green-300'>
                                Your ticket number:{' '}
                                <span className='font-mono font-bold'>{ticketNumber}</span>
                              </p>
                            </div>
                            <p className='text-xs text-gray-400'>
                              A confirmation email has been sent to {formData.email || 'your email'}
                            </p>
                          </div>
                        </motion.div>
                      )}

                      {submitStatus === 'error' && (
                        <motion.div
                          initial={{ opacity: 0, y: -10 }}
                          animate={{ opacity: 1, y: 0 }}
                          className='mb-6 p-4 bg-red-500/10 border border-red-500/20 rounded-xl'
                        >
                          <div className='flex items-start gap-3'>
                            <AlertCircle className='w-5 h-5 text-red-400 mt-0.5' />
                            <div>
                              <p className='text-sm font-semibold text-red-400 mb-1'>
                                Failed to create ticket
                              </p>
                              <p className='text-xs text-red-300'>
                                {errorMessage ||
                                  'Please try again or contact support@servereye.dev'}
                              </p>
                            </div>
                          </div>
                        </motion.div>
                      )}

                      <form onSubmit={handleSubmit} className='space-y-6'>
                        <div className='grid md:grid-cols-2 gap-6'>
                          <div>
                            <Input
                              label='Name'
                              value={formData.name}
                              onChange={e => setFormData({ ...formData, name: e.target.value })}
                              required
                              disabled={isSubmitting || isAuthenticated}
                              placeholder={isAuthenticated ? formData.name : 'Enter your name'}
                            />
                            {isAuthenticated && (
                              <p className='text-xs text-gray-400 mt-1'>
                                Name is locked to your account profile
                              </p>
                            )}
                          </div>
                          <div>
                            <Input
                              type='email'
                              label='Email'
                              value={formData.email}
                              onChange={e => setFormData({ ...formData, email: e.target.value })}
                              required
                              disabled={isSubmitting}
                              placeholder='Enter your email'
                            />
                          </div>
                        </div>

                        <Input
                          label='Subject'
                          value={formData.subject}
                          onChange={e => setFormData({ ...formData, subject: e.target.value })}
                          required
                          disabled={isSubmitting}
                        />

                        <div>
                          <label className='block text-sm font-medium mb-2'>
                            Message <span className='text-red-400'>*</span>
                          </label>
                          <textarea
                            value={formData.message}
                            onChange={e => setFormData({ ...formData, message: e.target.value })}
                            required
                            disabled={isSubmitting}
                            rows={6}
                            className='w-full px-4 py-3 bg-white/10 backdrop-blur-sm border border-white/20 rounded-xl text-white placeholder-gray-400 focus:outline-none focus:ring-2 focus:ring-blue-500 transition-all duration-200 disabled:opacity-50'
                            placeholder='Describe your issue or question...'
                          />
                        </div>

                        <Button type='submit' fullWidth isLoading={isSubmitting}>
                          <Send className='w-4 h-4 mr-2' />
                          Send Message
                        </Button>
                      </form>
                    </CardContent>
                  </Card>

                  {/* Contact Info */}
                  <div className='grid md:grid-cols-2 gap-6 mt-8'>
                    <Card>
                      <CardContent className='p-6'>
                        <div className='flex items-start gap-3'>
                          <Mail className='w-5 h-5 text-blue-400 mt-1' />
                          <div>
                            <p className='font-semibold mb-1'>Email Support</p>
                            <a
                              href='mailto:support@servereye.dev'
                              className='text-sm text-gray-400 hover:text-blue-400'
                            >
                              support@servereye.dev
                            </a>
                          </div>
                        </div>
                      </CardContent>
                    </Card>

                    <Card>
                      <CardContent className='p-6'>
                        <div className='flex items-start gap-3'>
                          <MessageCircle className='w-5 h-5 text-gray-400 mt-1' />
                          <div>
                            <p className='font-semibold mb-1'>Response Time</p>
                            <p className='text-sm text-gray-400'>Usually within 24 hours</p>
                          </div>
                        </div>
                      </CardContent>
                    </Card>
                  </div>
                </motion.div>
              ) : (
                <motion.div
                  key='tickets'
                  initial={{ opacity: 0, x: 20 }}
                  animate={{ opacity: 1, x: 0 }}
                  exit={{ opacity: 0, x: -20 }}
                >
                  <MyTickets />
                </motion.div>
              )}
            </AnimatePresence>
          </div>
        </div>
      </div>
    </main>
  );
}
