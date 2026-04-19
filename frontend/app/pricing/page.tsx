'use client';

import { motion, useScroll, useTransform, useMotionValue } from 'framer-motion';
import { Check, Star, ChevronRight, Sparkles, Zap, Shield, Crown } from 'lucide-react';
import Link from 'next/link';
import { Button } from '@/components/ui/Button';
import { Card, CardHeader, CardTitle, CardContent } from '@/components/ui/Card';
import { useEffect, useState, useRef } from 'react';
import { billingApi, SubscriptionPlan } from './billingApi';
import { logger } from '@/lib/telemetry/logger';
import { useAuth } from '@/context/AuthContext';
import { useSubscription } from '@/hooks/useSubscription';
import { cn } from '@/lib/utils';

enum PlanType {
  Free = 0,
  Lite = 1,
  Pro = 2,
  Enterprise = 3,
}

export default function PricingPage() {
  const { isAuthenticated } = useAuth();
  const { subscription } = useSubscription();
  const [plans, setPlans] = useState<SubscriptionPlan[]>([]);
  const [loading, setLoading] = useState(true);
  const [isYearly, setIsYearly] = useState(false);
  const [selectedPlan, setSelectedPlan] = useState<SubscriptionPlan | null>(null);
  const [showPaymentModal, setShowPaymentModal] = useState(false);
  const containerRef = useRef<HTMLDivElement>(null);
  const mouseX = useMotionValue(0);
  const mouseY = useMotionValue(0);

  const { scrollYProgress } = useScroll({
    target: containerRef,
    offset: ['start start', 'end start'],
  });

  const y = useTransform(scrollYProgress, [0, 1], [0, 100]);
  const opacity = useTransform(scrollYProgress, [0, 0.5], [1, 0]);

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
    // For non-authenticated users, always show Get Started for Free plan
    if (!isAuthenticated) {
      if (plan.planType === PlanType.Free) {
        return 'Get Started';
      }
      return 'Subscribe Now';
    }

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
    // For non-authenticated users, no plan is current
    if (!isAuthenticated) {
      return false;
    }

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
          cancelUrl: `${window.location.origin}/pricing?subscription=canceled`,
        });
        window.location.href = response.sessionUrl;
      } else if (method === 'yukassa') {
        // TODO: Implement YooKassa checkout
        alert('YooKassa coming soon!');
      }
    } catch (error) {
      logger.error('Failed to create checkout session', error as Error, {
        planId: selectedPlan.id,
      });
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
    <main className='min-h-screen bg-black text-white relative overflow-hidden'>
      <div
        ref={containerRef}
        onMouseMove={e => {
          mouseX.set(e.pageX);
          mouseY.set(e.pageY);
        }}
        className='absolute inset-0 bg-gradient-to-br from-blue-600/10 via-purple-600/10 to-pink-600/10'
      />
      <div className='absolute inset-0 bg-gradient-to-b from-transparent via-blue-900/5 to-transparent' />
      <div className='absolute top-1/2 left-1/2 transform -translate-x-1/2 -translate-y-1/2 w-[1000px] h-[1000px] bg-purple-500/10 rounded-full blur-3xl' />

      <div className='relative z-10'>
        <div className='container mx-auto px-6 py-32'>
          <motion.div
            style={{ y, opacity }}
            initial={{ opacity: 0, y: 30 }}
            whileInView={{ opacity: 1, y: 0 }}
            viewport={{ once: true }}
            transition={{ duration: 0.8 }}
            className='text-center max-w-3xl mx-auto mb-20'
          >
            <motion.div
              initial={{ opacity: 0, scale: 0.9 }}
              whileInView={{ opacity: 1, scale: 1 }}
              viewport={{ once: true }}
              transition={{ delay: 0.2 }}
              className='inline-flex items-center gap-2 px-4 py-2 bg-purple-500/10 border border-purple-500/20 rounded-full mb-6'
            >
              <Sparkles className='w-4 h-4 text-purple-400' />
              <span className='text-sm text-purple-300 font-medium'>
                Simple, transparent pricing
              </span>
            </motion.div>
            <h1 className='text-5xl md:text-6xl lg:text-7xl font-bold mb-6'>
              Choose Your
              <motion.span
                className='bg-clip-text text-transparent bg-gradient-to-r from-purple-400 via-pink-400 to-blue-400'
                animate={{
                  backgroundPosition: ['0% 50%', '100% 50%', '0% 50%'],
                }}
                transition={{
                  duration: 4,
                  repeat: Infinity,
                  ease: 'easeInOut',
                }}
                style={{ backgroundSize: '200% auto' }}
              >
                {' '}
                Plan
              </motion.span>
            </h1>
            <p className='text-xl text-gray-400'>Start free, scale as you grow. No hidden fees.</p>

            <div className='flex items-center justify-center gap-4 mt-8'>
              <span className={!isYearly ? 'text-white font-semibold' : 'text-gray-400'}>
                Monthly
              </span>
              <motion.button
                onClick={() => setIsYearly(!isYearly)}
                whileHover={{ scale: 1.05 }}
                whileTap={{ scale: 0.95 }}
                className='relative w-16 h-8 bg-gray-700 rounded-full transition-colors hover:bg-gray-600'
              >
                <motion.div
                  animate={{ x: isYearly ? 32 : 0 }}
                  transition={{ type: 'spring', stiffness: 500, damping: 30 }}
                  className='absolute top-1 left-1 w-6 h-6 bg-gradient-to-r from-purple-500 to-pink-500 rounded-full shadow-lg'
                />
              </motion.button>
              <span className={isYearly ? 'text-white font-semibold' : 'text-gray-400'}>
                Yearly <span className='text-green-400 text-sm'>(Save 15%)</span>
              </span>
            </div>
          </motion.div>

          {loading ? (
            <div className='text-center'>
              <motion.div
                animate={{ rotate: 360 }}
                transition={{ duration: 1, repeat: Infinity, ease: 'linear' }}
                className='w-12 h-12 border-4 border-purple-500 border-t-transparent rounded-full mx-auto'
              />
            </div>
          ) : (
            <div className='grid md:grid-cols-2 lg:grid-cols-4 gap-6 max-w-7xl mx-auto'>
              {plans?.map((plan, i) => {
                const price = getPrice(plan);
                const isPopular = plan.planType === PlanType.Pro;

                const getPlanIcon = () => {
                  switch (plan.planType) {
                    case PlanType.Free:
                      return <Shield className='w-6 h-6' />;
                    case PlanType.Lite:
                      return <Zap className='w-6 h-6' />;
                    case PlanType.Pro:
                      return <Star className='w-6 h-6' />;
                    case PlanType.Enterprise:
                      return <Crown className='w-6 h-6' />;
                    default:
                      return <Shield className='w-6 h-6' />;
                  }
                };

                const getPlanColor = () => {
                  switch (plan.planType) {
                    case PlanType.Free:
                      return 'from-green-500 to-emerald-500';
                    case PlanType.Lite:
                      return 'from-blue-500 to-cyan-500';
                    case PlanType.Pro:
                      return 'from-purple-500 to-pink-500';
                    case PlanType.Enterprise:
                      return 'from-yellow-500 to-orange-500';
                    default:
                      return 'from-gray-500 to-gray-600';
                  }
                };

                return (
                  <motion.div
                    key={plan.id}
                    initial={{ opacity: 0, y: 30, rotateX: -10 }}
                    whileInView={{ opacity: 1, y: 0, rotateX: 0 }}
                    viewport={{ once: true }}
                    transition={{ delay: i * 0.15, duration: 0.6 }}
                    whileHover={{
                      scale: 1.05,
                      y: -10,
                      rotateX: 5,
                      boxShadow: '0 25px 50px rgba(168, 85, 247, 0.3)',
                    }}
                    className='relative h-full'
                    style={{ transformStyle: 'preserve-3d' }}
                  >
                    <Card
                      className={cn(
                        'h-full bg-gray-900/50 border border-white/10 backdrop-blur-xl',
                        isPopular ? 'border-purple-500/50 relative' : '',
                        isCurrentPlan(plan) &&
                          'border-green-500/50 bg-gradient-to-br from-green-500/10 to-emerald-500/10',
                      )}
                    >
                      <motion.div className='absolute inset-0 bg-gradient-to-br from-purple-600/10 to-pink-600/10 opacity-0 hover:opacity-100 transition-opacity rounded-lg' />
                      {isPopular && !isCurrentPlan(plan) && (
                        <motion.div
                          animate={{ scale: [1, 1.05, 1] }}
                          transition={{ duration: 2, repeat: Infinity, ease: 'easeInOut' }}
                          className='absolute -top-10 left-1/2 transform -translate-x-1/2 px-4 py-1 bg-gradient-to-r from-purple-600 to-pink-600 rounded-full text-sm font-semibold z-10'
                        >
                          Most Popular
                        </motion.div>
                      )}
                      {isCurrentPlan(plan) && (
                        <div className='absolute -top-10 left-1/2 transform -translate-x-1/2 px-4 py-1 bg-gradient-to-r from-green-600 to-emerald-600 rounded-full text-sm font-semibold z-10 flex items-center gap-1'>
                          <Check className='w-3 h-3' />
                          Current Plan
                        </div>
                      )}
                      <CardHeader>
                        <motion.div
                          animate={{ rotate: [0, 360] }}
                          transition={{ duration: 20, repeat: Infinity, ease: 'linear' }}
                          className={`w-14 h-14 bg-gradient-to-br ${getPlanColor()} rounded-2xl flex items-center justify-center mb-4 text-white shadow-lg`}
                        >
                          {getPlanIcon()}
                        </motion.div>
                        <CardTitle className='text-2xl'>{plan.name}</CardTitle>
                        <p className='text-gray-400 mt-2'>{plan.description}</p>
                        <div className='mt-6'>
                          {plan.planType === PlanType.Enterprise ? (
                            <div className='text-4xl font-bold bg-clip-text text-transparent bg-gradient-to-r from-yellow-400 to-orange-400'>
                              Custom
                            </div>
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
                        {plan.planType === PlanType.Enterprise ? (
                          <Link href='/contact'>
                            <motion.div whileHover={{ scale: 1.05 }} whileTap={{ scale: 0.95 }}>
                              <Button
                                variant={isPopular ? 'primary' : 'secondary'}
                                fullWidth
                                className='mb-6 shadow-lg shadow-purple-500/30'
                              >
                                Let's talk
                              </Button>
                            </motion.div>
                          </Link>
                        ) : (
                          <motion.div whileHover={{ scale: 1.05 }} whileTap={{ scale: 0.95 }}>
                            <Button
                              variant={isPopular ? 'primary' : 'secondary'}
                              fullWidth
                              className='mb-6 shadow-lg shadow-purple-500/30'
                              onClick={() => handleSubscribe(plan)}
                              disabled={subscription?.planType === plan.planType}
                            >
                              {getButtonText(plan)}
                            </Button>
                          </motion.div>
                        )}
                        <div className='space-y-3'>
                          {plan.planType === PlanType.Enterprise
                            ? plan.features.map((feature, j) => (
                                <motion.div
                                  key={j}
                                  initial={{ opacity: 0, x: -10 }}
                                  whileInView={{ opacity: 1, x: 0 }}
                                  viewport={{ once: true }}
                                  transition={{ delay: j * 0.05 }}
                                  className='flex items-start gap-3'
                                >
                                  <Check className='w-5 h-5 text-green-400 flex-shrink-0 mt-0.5' />
                                  <span className='text-gray-300'>{feature}</span>
                                </motion.div>
                              ))
                            : getFeatures(plan).map((feature, j) => (
                                <motion.div
                                  key={j}
                                  initial={{ opacity: 0, x: -10 }}
                                  whileInView={{ opacity: 1, x: 0 }}
                                  viewport={{ once: true }}
                                  transition={{ delay: j * 0.05 }}
                                  className='flex items-start gap-3'
                                >
                                  <Check className='w-5 h-5 text-green-400 flex-shrink-0 mt-0.5' />
                                  <span className='text-gray-300'>{feature}</span>
                                </motion.div>
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
            initial={{ opacity: 0, y: 30 }}
            whileInView={{ opacity: 1, y: 0 }}
            viewport={{ once: true }}
            transition={{ duration: 0.8 }}
            className='mt-32 max-w-3xl mx-auto'
          >
            <motion.div
              initial={{ opacity: 0, scale: 0.9 }}
              whileInView={{ opacity: 1, scale: 1 }}
              viewport={{ once: true }}
              transition={{ delay: 0.2 }}
              className='text-center mb-12'
            >
              <h2 className='text-4xl md:text-5xl font-bold mb-4'>
                Frequently Asked
                <span className='bg-clip-text text-transparent bg-gradient-to-r from-purple-400 to-pink-400'>
                  {' '}
                  Questions
                </span>
              </h2>
              <p className='text-gray-400'>Everything you need to know about our pricing</p>
            </motion.div>
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
                <motion.div
                  key={i}
                  initial={{ opacity: 0, y: 20 }}
                  whileInView={{ opacity: 1, y: 0 }}
                  viewport={{ once: true }}
                  transition={{ delay: i * 0.1 }}
                  whileHover={{ scale: 1.02, y: -5 }}
                  className='group'
                >
                  <Card className='bg-gray-900/50 border border-white/10 hover:border-purple-500/50 transition-all duration-300'>
                    <CardContent className='pt-6'>
                      <h3 className='text-lg font-semibold mb-2 group-hover:text-purple-300 transition-colors'>
                        {faq.q}
                      </h3>
                      <p className='text-gray-400 group-hover:text-gray-300 transition-colors'>
                        {faq.a}
                      </p>
                    </CardContent>
                  </Card>
                </motion.div>
              ))}
            </div>
          </motion.div>

          {/* CTA */}
          <motion.div
            initial={{ opacity: 0, y: 30 }}
            whileInView={{ opacity: 1, y: 0 }}
            viewport={{ once: true }}
            transition={{ duration: 0.8 }}
            className='mt-32 text-center'
          >
            <motion.div whileHover={{ scale: 1.02 }} transition={{ duration: 0.3 }}>
              <Card className='bg-gradient-to-br from-purple-600/10 to-pink-600/10 border border-purple-500/30 backdrop-blur-xl'>
                <CardContent className='py-16'>
                  <motion.div
                    animate={{ rotate: [0, 10, -10, 0] }}
                    transition={{ duration: 4, repeat: Infinity, ease: 'easeInOut' }}
                    className='w-16 h-16 mx-auto mb-6 bg-gradient-to-br from-purple-500 to-pink-500 rounded-2xl flex items-center justify-center shadow-2xl shadow-purple-500/50'
                  >
                    <Sparkles className='w-8 h-8 text-white' />
                  </motion.div>
                  <h3 className='text-4xl font-bold mb-4'>
                    Ready to get
                    <span className='bg-clip-text text-transparent bg-gradient-to-r from-purple-400 to-pink-400'>
                      {' '}
                      started?
                    </span>
                  </h3>
                  <p className='text-gray-400 mb-8 max-w-2xl mx-auto text-lg'>
                    Join thousands of developers monitoring their infrastructure with ServerEye
                  </p>
                  <div className='flex flex-col sm:flex-row gap-4 justify-center'>
                    <motion.div whileHover={{ scale: 1.05 }} whileTap={{ scale: 0.95 }}>
                      <Link href='/register'>
                        <Button size='lg' className='shadow-lg shadow-purple-500/30'>
                          Get Started
                        </Button>
                      </Link>
                    </motion.div>
                    <motion.div whileHover={{ scale: 1.05 }} whileTap={{ scale: 0.95 }}>
                      <Link href='/contact'>
                        <Button variant='secondary' size='lg' className='shadow-lg'>
                          Contact Sales
                        </Button>
                      </Link>
                    </motion.div>
                  </div>
                </CardContent>
              </Card>
            </motion.div>
          </motion.div>
        </div>
      </div>

      {/* Payment Method Modal */}
      {showPaymentModal && selectedPlan && (
        <motion.div
          initial={{ opacity: 0 }}
          animate={{ opacity: 1 }}
          exit={{ opacity: 0 }}
          className='fixed inset-0 bg-black/80 backdrop-blur-sm flex items-center justify-center z-50'
        >
          <motion.div
            initial={{ opacity: 0, scale: 0.9, y: 20 }}
            animate={{ opacity: 1, scale: 1, y: 0 }}
            exit={{ opacity: 0, scale: 0.9, y: 20 }}
            transition={{ duration: 0.3 }}
            className='bg-gray-900/95 border border-purple-500/30 rounded-2xl p-8 max-w-md w-full mx-4 backdrop-blur-xl shadow-2xl shadow-purple-500/20'
          >
            <motion.div
              animate={{ rotate: [0, 10, -10, 0] }}
              transition={{ duration: 4, repeat: Infinity, ease: 'easeInOut' }}
              className='w-12 h-12 mx-auto mb-4 bg-gradient-to-br from-purple-500 to-pink-500 rounded-xl flex items-center justify-center'
            >
              <Sparkles className='w-6 h-6 text-white' />
            </motion.div>
            <h3 className='text-2xl font-bold mb-2 text-center'>Choose Payment Method</h3>
            <p className='text-gray-400 mb-6 text-center'>
              Select how you'd like to pay for the {selectedPlan.name} plan
              {isYearly && ' (yearly)'}
            </p>

            <div className='space-y-3'>
              <motion.button
                whileHover={{ scale: 1.02, x: 5 }}
                whileTap={{ scale: 0.98 }}
                onClick={() => handlePaymentMethod('stripe')}
                className='w-full flex items-center justify-between p-4 bg-gray-800/50 border border-white/10 hover:border-purple-500/50 rounded-xl transition-all duration-300 group'
              >
                <div className='flex items-center gap-3'>
                  <motion.div
                    whileHover={{ rotate: 360 }}
                    transition={{ duration: 0.5 }}
                    className='w-12 h-12 bg-gradient-to-br from-blue-600 to-purple-600 rounded-xl flex items-center justify-center text-white font-bold text-xl shadow-lg'
                  >
                    S
                  </motion.div>
                  <div className='text-left'>
                    <div className='font-semibold group-hover:text-purple-300 transition-colors'>
                      Credit Card
                    </div>
                    <div className='text-sm text-gray-400'>Visa, Mastercard, Amex</div>
                  </div>
                </div>
                <ChevronRight className='w-5 h-5 text-gray-400 group-hover:text-purple-400 group-hover:translate-x-1 transition-all' />
              </motion.button>

              <motion.button
                whileHover={{ scale: 1.02, x: 5 }}
                whileTap={{ scale: 0.98 }}
                onClick={() => handlePaymentMethod('yukassa')}
                className='w-full flex items-center justify-between p-4 bg-gray-800/50 border border-white/10 hover:border-purple-500/50 rounded-xl transition-all duration-300 group'
              >
                <div className='flex items-center gap-3'>
                  <motion.div
                    whileHover={{ rotate: 360 }}
                    transition={{ duration: 0.5 }}
                    className='w-12 h-12 bg-gradient-to-br from-white to-gray-200 rounded-xl flex items-center justify-center text-blue-600 font-bold text-xl shadow-lg border border-gray-300'
                  >
                    Ю
                  </motion.div>
                  <div className='text-left'>
                    <div className='font-semibold group-hover:text-purple-300 transition-colors'>
                      YooKassa
                    </div>
                    <div className='text-sm text-gray-400'>Карты, СБП, QIWI</div>
                  </div>
                </div>
                <ChevronRight className='w-5 h-5 text-gray-400 group-hover:text-purple-400 group-hover:translate-x-1 transition-all' />
              </motion.button>
            </div>

            <motion.button
              whileHover={{ scale: 1.05 }}
              whileTap={{ scale: 0.95 }}
              onClick={() => {
                setShowPaymentModal(false);
                setSelectedPlan(null);
              }}
              className='w-full mt-6 py-3 text-gray-400 hover:text-white transition-colors font-medium'
            >
              Cancel
            </motion.button>
          </motion.div>
        </motion.div>
      )}
    </main>
  );
}
