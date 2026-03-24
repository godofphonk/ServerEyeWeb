'use client';

import { motion } from 'framer-motion';
import { Check, Zap, Star, ChevronRight } from 'lucide-react';
import StripeIcon from './stripe-icon.svg';
import Link from 'next/link';
import { Button } from '@/components/ui/Button';
import { Card, CardHeader, CardTitle, CardContent } from '@/components/ui/Card';
import { useEffect, useState } from 'react';
import { billingApi, SubscriptionPlan } from './billingApi';
import { logger } from '@/lib/telemetry/logger';
import { useAuth } from '@/context/AuthContext';
import { useSubscription } from '@/hooks/useSubscription';
import { cn } from '@/lib/utils';

enum PlanType {
  Free = 0,
  Pro = 1,
  Enterprise = 2
}

export default function PricingPage() {
  const { isAuthenticated } = useAuth();
  const { subscription } = useSubscription();
  const [plans, setPlans] = useState<SubscriptionPlan[]>([]);
  const [loading, setLoading] = useState(true);
  const [isYearly, setIsYearly] = useState(false);
  const [selectedPlan, setSelectedPlan] = useState<SubscriptionPlan | null>(null);
  const [showPaymentModal, setShowPaymentModal] = useState(false);

  useEffect(() => {
    loadPlans();
  }, []);

  const loadPlans = async () => {
    try {
      const data = await billingApi.getPlans();
      setPlans(data);
    } catch (error) {
      logger.error('Failed to load billing plans', error as Error);
    } finally {
      setLoading(false);
    }
  };

  const handleSubscribe = (plan: SubscriptionPlan) => {
    if (!isAuthenticated) {
      window.location.href = '/auth/login';
      return;
    }

    if (plan.planType === PlanType.Enterprise) {
      window.location.href = '/contact';
      return;
    }

    // Show payment method selection modal
    setSelectedPlan(plan);
    setShowPaymentModal(true);
  };

  const getButtonText = (plan: SubscriptionPlan) => {
    // For Free plan, check if user has no active subscription
    if (plan.planType === PlanType.Free) {
      if (!subscription || subscription.planType === PlanType.Free) {
        return 'Current Plan';
      }
      return 'Get Started';
    }
    
    if (plan.planType === PlanType.Enterprise) {
      return 'Contact Sales';
    }

    // For Pro plan, check if user already has this plan
    if (subscription && subscription.planType === plan.planType) {
      return 'Current Plan';
    }

    return 'Subscribe Now';
  };

  const isCurrentPlan = (plan: SubscriptionPlan) => {
    // Free plan is current if user has no subscription or has Free plan
    if (plan.planType === PlanType.Free) {
      return !subscription || subscription.planType === PlanType.Free;
    }
    
    // For paid plans, check if user has this specific plan
    return subscription?.planType === plan.planType;
  };

  const handlePaymentMethod = async (method: 'stripe' | 'yukassa') => {
    if (!selectedPlan) return;

    try {
      if (method === 'stripe') {
        const response = await billingApi.createCheckout({
          planType: selectedPlan.planType,
          isYearly,
          successUrl: `${window.location.origin}/dashboard?subscription=success`,
          cancelUrl: `${window.location.origin}/pricing?subscription=canceled`
        });
        window.location.href = response.sessionUrl;
      } else if (method === 'yukassa') {
        // TODO: Implement YooKassa checkout
        alert('YooKassa coming soon!');
      }
    } catch (error) {
      logger.error('Failed to create checkout session', error as Error, { planId: selectedPlan.id });
      alert('Failed to start checkout. Please try again.');
    } finally {
      setShowPaymentModal(false);
      setSelectedPlan(null);
    }
  };

  const getPrice = (plan: SubscriptionPlan) => {
    return isYearly ? plan.yearlyPrice : plan.monthlyPrice;
  };

  const getFeatures = (plan: SubscriptionPlan) => {
    const features = [
      `Up to ${plan.maxServers === 999 ? 'unlimited' : plan.maxServers} servers`,
      `${plan.metricsRetentionDays}-day data retention`,
    ];

    if (plan.hasAlerts) features.push('Custom alerts');
    if (plan.hasApiAccess) features.push('API access');
    if (plan.hasPrioritySupport) features.push('Priority support');

    return [...features, ...plan.features];
  };

  return (
    <main className='min-h-screen bg-black text-white'>
      <div className='absolute inset-0 bg-gradient-to-br from-blue-600/10 via-purple-600/10 to-pink-600/10' />

      <div className='relative z-10'>
        <div className='container mx-auto px-6 py-20'>
          <motion.div
            initial={{ opacity: 0, y: 20 }}
            animate={{ opacity: 1, y: 0 }}
            className='text-center max-w-3xl mx-auto mb-16'
          >
            <div className='inline-flex items-center gap-2 px-4 py-2 bg-purple-500/10 border border-purple-500/20 rounded-full mb-6'>
              <Star className='w-4 h-4 text-purple-400' />
              <span className='text-sm text-purple-400'>Simple, transparent pricing</span>
            </div>
            <h1 className='text-5xl md:text-6xl font-bold mb-6'>Choose Your Plan</h1>
            <p className='text-xl text-gray-400'>Start free, scale as you grow. No hidden fees.</p>

            <div className='flex items-center justify-center gap-4 mt-8'>
              <span className={!isYearly ? 'text-white font-semibold' : 'text-gray-400'}>Monthly</span>
              <button
                onClick={() => setIsYearly(!isYearly)}
                className='relative w-14 h-7 bg-gray-700 rounded-full transition-colors hover:bg-gray-600'
              >
                <div
                  className={`absolute top-1 left-1 w-5 h-5 bg-white rounded-full transition-transform ${
                    isYearly ? 'transform translate-x-7' : ''
                  }`}
                />
              </button>
              <span className={isYearly ? 'text-white font-semibold' : 'text-gray-400'}>
                Yearly <span className='text-green-400 text-sm'>(Save 15%)</span>
              </span>
            </div>
          </motion.div>

          {loading ? (
            <div className='text-center'>Loading plans...</div>
          ) : (
            <div className='grid md:grid-cols-3 gap-8 max-w-6xl mx-auto'>
              {plans?.map((plan, i) => {
                const price = getPrice(plan);
                const isPopular = plan.planType === PlanType.Pro;

                return (
                  <motion.div
                    key={plan.id}
                    initial={{ opacity: 0, y: 20 }}
                    animate={{ opacity: 1, y: 0 }}
                    transition={{ delay: i * 0.1 }}
                    className='relative'
                  >
                    <Card className={cn(
                      isPopular ? 'border-blue-500/50 relative' : '',
                      isCurrentPlan(plan) && 'border-green-500/50 bg-gradient-to-br from-green-500/5 to-emerald-500/5'
                    )}>
                      {isPopular && !isCurrentPlan(plan) && (
                        <div className='absolute -top-10 left-1/2 transform -translate-x-1/2 px-4 py-1 bg-gradient-to-r from-blue-600 to-purple-600 rounded-full text-sm font-semibold z-10'>
                          Most Popular
                        </div>
                      )}
                      {isCurrentPlan(plan) && (
                        <div className='absolute -top-10 left-1/2 transform -translate-x-1/2 px-4 py-1 bg-gradient-to-r from-green-600 to-emerald-600 rounded-full text-sm font-semibold z-10 flex items-center gap-1'>
                          <Check className='w-3 h-3' />
                          Current Plan
                        </div>
                      )}
                      <CardHeader>
                        <CardTitle className='text-2xl'>{plan.name}</CardTitle>
                        <p className='text-gray-400 mt-2'>{plan.description}</p>
                        <div className='mt-6'>
                          {plan.planType === PlanType.Enterprise ? (
                            <div className='text-4xl font-bold'>Custom</div>
                          ) : (
                            <div className='text-5xl font-bold'>
                              ${price}
                              <span className='text-xl text-gray-400 font-normal'>
                                /{isYearly ? 'year' : 'month'}
                              </span>
                            </div>
                          )}
                        </div>
                      </CardHeader>
                      <CardContent>
                        <Button
                          variant={isPopular ? 'primary' : 'secondary'}
                          fullWidth
                          className='mb-6'
                          onClick={() => handleSubscribe(plan)}
                          disabled={subscription?.planType === plan.planType}
                        >
                          {getButtonText(plan)}
                        </Button>
                        <div className='space-y-3'>
                          {getFeatures(plan).map((feature, j) => (
                            <div key={j} className='flex items-start gap-3'>
                              <Check className='w-5 h-5 text-green-400 flex-shrink-0 mt-0.5' />
                              <span className='text-gray-300'>{feature}</span>
                            </div>
                          ))}
                        </div>
                      </CardContent>
                    </Card>
                  </motion.div>
                );
              })}
            </div>
          )}

          {/* FAQ */}
          <motion.div
            initial={{ opacity: 0, y: 20 }}
            animate={{ opacity: 1, y: 0 }}
            transition={{ delay: 0.5 }}
            className='mt-24 max-w-3xl mx-auto'
          >
            <h2 className='text-3xl font-bold text-center mb-12'>Frequently Asked Questions</h2>
            <div className='space-y-6'>
              {[
                {
                  q: 'Can I change plans later?',
                  a: 'Yes, you can upgrade or downgrade your plan at any time. Changes take effect immediately.',
                },
                {
                  q: 'What payment methods do you accept?',
                  a: 'We accept all major credit cards, PayPal, and wire transfers for Enterprise plans.',
                },
                {
                  q: 'Is there a free trial?',
                  a: 'Yes, Pro plan comes with a 14-day free trial. No credit card required.',
                },
                {
                  q: 'What happens if I exceed my server limit?',
                  a: "You'll be notified and can either upgrade your plan or remove servers to stay within your limit.",
                },
              ].map((faq, i) => (
                <Card key={i}>
                  <CardContent className='pt-6'>
                    <h3 className='text-lg font-semibold mb-2'>{faq.q}</h3>
                    <p className='text-gray-400'>{faq.a}</p>
                  </CardContent>
                </Card>
              ))}
            </div>
          </motion.div>

          {/* CTA */}
          <motion.div
            initial={{ opacity: 0, y: 20 }}
            animate={{ opacity: 1, y: 0 }}
            transition={{ delay: 0.7 }}
            className='mt-24 text-center'
          >
            <Card>
              <CardContent className='py-12'>
                <h3 className='text-3xl font-bold mb-4'>Ready to get started?</h3>
                <p className='text-gray-400 mb-8 max-w-2xl mx-auto'>
                  Join thousands of developers monitoring their infrastructure with ServerEye
                </p>
                <div className='flex flex-col sm:flex-row gap-4 justify-center'>
                  <Link href='/register'>
                    <Button size='lg'>Get Started</Button>
                  </Link>
                  <Link href='/contact'>
                    <Button variant='secondary' size='lg'>
                      Contact Sales
                    </Button>
                  </Link>
                </div>
              </CardContent>
            </Card>
          </motion.div>
        </div>
      </div>

      {/* Payment Method Modal */}
      {showPaymentModal && selectedPlan && (
        <div className='fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center z-50'>
          <div className='bg-gray-900 rounded-xl p-6 max-w-md w-full mx-4 border border-gray-700'>
            <h3 className='text-2xl font-bold mb-2'>Choose Payment Method</h3>
            <p className='text-gray-400 mb-6'>
              Select how you'd like to pay for the {selectedPlan.name} plan
              {isYearly && ' (yearly)'}
            </p>
            
            <div className='space-y-3'>
              <button
                onClick={() => handlePaymentMethod('stripe')}
                className='w-full flex items-center justify-between p-4 bg-gray-800 hover:bg-gray-700 rounded-lg transition-colors'
              >
                <div className='flex items-center gap-3'>
                  <div className='w-10 h-10 bg-blue-600 rounded-full flex items-center justify-center text-white font-bold text-2xl'>
                    S
                  </div>
                  <div className='text-left'>
                    <div className='font-semibold'>Credit Card</div>
                    <div className='text-sm text-gray-400'>Visa, Mastercard, Amex</div>
                  </div>
                </div>
                <ChevronRight className='w-5 h-5 text-gray-400' />
              </button>

              <button
                onClick={() => handlePaymentMethod('yukassa')}
                className='w-full flex items-center justify-between p-4 bg-gray-800 hover:bg-gray-700 rounded-lg transition-colors'
              >
                <div className='flex items-center gap-3'>
                  <div className='w-10 h-10 bg-white rounded-full flex items-center justify-center text-blue-600 font-bold text-2xl border border-gray-300'>
                    Ю
                  </div>
                  <div className='text-left'>
                    <div className='font-semibold'>YooKassa</div>
                    <div className='text-sm text-gray-400'>Карты, СБП, QIWI</div>
                  </div>
                </div>
                <ChevronRight className='w-5 h-5 text-gray-400' />
              </button>
            </div>

            <button
              onClick={() => {
                setShowPaymentModal(false);
                setSelectedPlan(null);
              }}
              className='w-full mt-6 py-2 text-gray-400 hover:text-white transition-colors'
            >
              Cancel
            </button>
          </div>
        </div>
      )}
    </main>
  );
}
