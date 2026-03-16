'use client';

import { motion } from 'framer-motion';
import { Check, Zap, Star } from 'lucide-react';
import Link from 'next/link';
import { Button } from '@/components/ui/Button';
import { Card, CardHeader, CardTitle, CardContent } from '@/components/ui/Card';
import { useEffect, useState } from 'react';
import { billingApi, SubscriptionPlan } from '@/lib/billingApi';
import { useAuth } from '@/contexts/AuthContext';

enum PlanType {
  Free = 0,
  Basic = 1,
  Pro = 2,
  Enterprise = 3
}

export default function PricingPage() {
  const { isAuthenticated } = useAuth();
  const [plans, setPlans] = useState<SubscriptionPlan[]>([]);
  const [loading, setLoading] = useState(true);
  const [isYearly, setIsYearly] = useState(false);

  useEffect(() => {
    loadPlans();
  }, []);

  const loadPlans = async () => {
    try {
      const data = await billingApi.getPlans();
      setPlans(data);
    } catch (error) {
      console.error('Failed to load plans:', error);
    } finally {
      setLoading(false);
    }
  };

  const handleSubscribe = async (plan: SubscriptionPlan) => {
    if (!isAuthenticated) {
      window.location.href = '/auth?redirect=/pricing';
      return;
    }

    if (plan.planType === PlanType.Free) {
      window.location.href = '/dashboard';
      return;
    }

    if (plan.planType === PlanType.Enterprise) {
      window.location.href = '/contact';
      return;
    }

    try {
      const response = await billingApi.createCheckout({
        planType: plan.planType,
        isYearly,
        successUrl: `${window.location.origin}/dashboard?subscription=success`,
        cancelUrl: `${window.location.origin}/pricing?subscription=canceled`
      });

      window.location.href = response.sessionUrl;
    } catch (error) {
      console.error('Failed to create checkout:', error);
      alert('Failed to start checkout. Please try again.');
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
              {plans.map((plan, i) => {
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
                    {isPopular && (
                      <div className='absolute -top-4 left-1/2 transform -translate-x-1/2 px-4 py-1 bg-gradient-to-r from-blue-600 to-purple-600 rounded-full text-sm font-semibold'>
                        Most Popular
                      </div>
                    )}
                    <Card className={isPopular ? 'border-blue-500/50' : ''}>
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
                        >
                          {plan.planType === PlanType.Free
                            ? 'Get Started'
                            : plan.planType === PlanType.Enterprise
                            ? 'Contact Sales'
                            : 'Subscribe Now'}
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
    </main>
  );
}
