"use client";

import { motion } from "framer-motion";
import { Check, Zap, Star } from "lucide-react";
import Link from "next/link";
import { Button } from "@/components/ui/Button";
import { Card, CardHeader, CardTitle, CardContent } from "@/components/ui/Card";

const plans = [
  {
    name: "Free",
    price: 0,
    description: "Perfect for personal projects",
    features: [
      "Up to 3 servers",
      "Basic metrics",
      "7-day data retention",
      "Email support",
      "Community access",
    ],
    cta: "Go to Dashboard",
    popular: false,
  },
  {
    name: "Pro",
    price: 29,
    description: "For growing teams",
    features: [
      "Up to 25 servers",
      "Advanced metrics",
      "30-day data retention",
      "Priority support",
      "Custom alerts",
      "API access",
      "Team collaboration",
    ],
    cta: "Buy Now",
    popular: false,
  },
  {
    name: "Enterprise",
    price: null,
    description: "For large organizations",
    features: [
      "Unlimited servers",
      "All metrics",
      "Unlimited data retention",
      "24/7 dedicated support",
      "Custom integrations",
      "SLA guarantee",
      "On-premise option",
      "Advanced security",
    ],
    cta: "Custom Plan",
    popular: false,
  },
];

export default function PricingPage() {
  return (
    <main className="min-h-screen bg-black text-white">
      <div className="absolute inset-0 bg-gradient-to-br from-blue-600/10 via-purple-600/10 to-pink-600/10" />
      
      <div className="relative z-10">
        {/* Header */}
        <div className="container mx-auto px-6 py-20">
          <motion.div
            initial={{ opacity: 0, y: 20 }}
            animate={{ opacity: 1, y: 0 }}
            className="text-center max-w-3xl mx-auto mb-16"
          >
            <div className="inline-flex items-center gap-2 px-4 py-2 bg-purple-500/10 border border-purple-500/20 rounded-full mb-6">
              <Star className="w-4 h-4 text-purple-400" />
              <span className="text-sm text-purple-400">Simple, transparent pricing</span>
            </div>
            <h1 className="text-5xl md:text-6xl font-bold mb-6">
              Choose Your Plan
            </h1>
            <p className="text-xl text-gray-400">
              Start free, scale as you grow. No hidden fees.
            </p>
          </motion.div>

          {/* Pricing Cards */}
          <div className="grid md:grid-cols-3 gap-8 max-w-6xl mx-auto">
            {plans.map((plan, i) => (
              <motion.div
                key={i}
                initial={{ opacity: 0, y: 20 }}
                animate={{ opacity: 1, y: 0 }}
                transition={{ delay: i * 0.1 }}
                className="relative"
              >
                {plan.popular && (
                  <div className="absolute -top-4 left-1/2 transform -translate-x-1/2 px-4 py-1 bg-gradient-to-r from-blue-600 to-purple-600 rounded-full text-sm font-semibold">
                    Most Popular
                  </div>
                )}
                <Card className={plan.popular ? "border-blue-500/50" : ""}>
                  <CardHeader>
                    <CardTitle className="text-2xl">{plan.name}</CardTitle>
                    <p className="text-gray-400 mt-2">{plan.description}</p>
                    <div className="mt-6">
                      {plan.price === null ? (
                        <div className="text-4xl font-bold">Custom</div>
                      ) : (
                        <>
                          <div className="text-5xl font-bold">
                            ${plan.price}
                            <span className="text-xl text-gray-400 font-normal">/month</span>
                          </div>
                        </>
                      )}
                    </div>
                  </CardHeader>
                  <CardContent>
                    <Button
                      variant={plan.name === "Pro" || plan.name === "Enterprise" ? "primary" : "secondary"}
                      fullWidth
                      className="mb-6"
                      onClick={() => {
                        if (plan.name === "Free") {
                          window.location.href = "/dashboard";
                        } else if (plan.name === "Pro") {
                          // Handle Pro plan purchase
                          console.log("Buy Pro plan");
                        } else if (plan.name === "Enterprise") {
                          // Handle Enterprise plan
                          console.log("Custom Enterprise plan");
                        }
                      }}
                    >
                      {plan.cta}
                    </Button>
                    <div className="space-y-3">
                      {plan.features.map((feature, j) => (
                        <div key={j} className="flex items-start gap-3">
                          <Check className="w-5 h-5 text-green-400 flex-shrink-0 mt-0.5" />
                          <span className="text-gray-300">{feature}</span>
                        </div>
                      ))}
                    </div>
                  </CardContent>
                </Card>
              </motion.div>
            ))}
          </div>

          {/* FAQ */}
          <motion.div
            initial={{ opacity: 0, y: 20 }}
            animate={{ opacity: 1, y: 0 }}
            transition={{ delay: 0.5 }}
            className="mt-24 max-w-3xl mx-auto"
          >
            <h2 className="text-3xl font-bold text-center mb-12">
              Frequently Asked Questions
            </h2>
            <div className="space-y-6">
              {[
                {
                  q: "Can I change plans later?",
                  a: "Yes, you can upgrade or downgrade your plan at any time. Changes take effect immediately.",
                },
                {
                  q: "What payment methods do you accept?",
                  a: "We accept all major credit cards, PayPal, and wire transfers for Enterprise plans.",
                },
                {
                  q: "Is there a free trial?",
                  a: "Yes, Pro plan comes with a 14-day free trial. No credit card required.",
                },
                {
                  q: "What happens if I exceed my server limit?",
                  a: "You'll be notified and can either upgrade your plan or remove servers to stay within your limit.",
                },
              ].map((faq, i) => (
                <Card key={i}>
                  <CardContent className="pt-6">
                    <h3 className="text-lg font-semibold mb-2">{faq.q}</h3>
                    <p className="text-gray-400">{faq.a}</p>
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
            className="mt-24 text-center"
          >
            <Card>
              <CardContent className="py-12">
                <h3 className="text-3xl font-bold mb-4">
                  Ready to get started?
                </h3>
                <p className="text-gray-400 mb-8 max-w-2xl mx-auto">
                  Join thousands of developers monitoring their infrastructure with ServerEye
                </p>
                <div className="flex flex-col sm:flex-row gap-4 justify-center">
                  <Link href="/register">
                    <Button size="lg">
                      Get Started
                    </Button>
                  </Link>
                  <Link href="/contact">
                    <Button variant="secondary" size="lg">
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
