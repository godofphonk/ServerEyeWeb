"use client";

import { motion } from "framer-motion";
import { Shield, Lock, Key, Eye } from "lucide-react";

export default function Security() {
  const features = [
    {
      icon: Shield,
      title: "End-to-End Encryption",
      description: "All data transmitted between your servers and our platform is encrypted with TLS 1.3",
    },
    {
      icon: Lock,
      title: "Zero Trust Architecture",
      description: "Your servers never expose ports. Agent connects outbound via HTTPS only",
    },
    {
      icon: Key,
      title: "JWT Authentication",
      description: "Secure authentication with industry-standard JSON Web Tokens",
    },
    {
      icon: Eye,
      title: "No Data Retention",
      description: "We don't store your sensitive data. Metrics are kept for 30 days only",
    },
  ];

  return (
    <section className="py-32 relative">
      <div className="container mx-auto px-6">
        <motion.div
          initial={{ opacity: 0, y: 20 }}
          whileInView={{ opacity: 1, y: 0 }}
          viewport={{ once: true }}
          className="text-center mb-20"
        >
          <div className="inline-block px-4 py-2 bg-green-500/10 border border-green-500/20 rounded-full mb-6">
            <span className="text-green-400 font-semibold">🔒 Enterprise-Grade Security</span>
          </div>
          <h2 className="text-5xl md:text-6xl font-bold mb-6">
            Your Data Is
            <span className="bg-clip-text text-transparent bg-gradient-to-r from-green-400 to-emerald-400">
              {" "}Safe With Us
            </span>
          </h2>
          <p className="text-xl text-gray-400 max-w-2xl mx-auto">
            Security is our top priority. We follow industry best practices to protect your infrastructure.
          </p>
        </motion.div>

        <div className="grid grid-cols-1 md:grid-cols-2 gap-8 max-w-5xl mx-auto">
          {features.map((feature, i) => (
            <motion.div
              key={i}
              initial={{ opacity: 0, y: 20 }}
              whileInView={{ opacity: 1, y: 0 }}
              viewport={{ once: true }}
              transition={{ delay: i * 0.1 }}
              className="group bg-gradient-to-br from-gray-900 to-black border border-white/10 rounded-2xl p-8 hover:border-green-500/50 transition-all duration-300"
            >
              <div className="w-14 h-14 bg-green-500/10 border border-green-500/20 rounded-xl flex items-center justify-center mb-6 group-hover:scale-110 transition-transform">
                <feature.icon className="w-7 h-7 text-green-400" />
              </div>
              <h3 className="text-2xl font-bold mb-3">{feature.title}</h3>
              <p className="text-gray-400 leading-relaxed">{feature.description}</p>
            </motion.div>
          ))}
        </div>

        {/* Security badges */}
        <motion.div
          initial={{ opacity: 0 }}
          whileInView={{ opacity: 1 }}
          viewport={{ once: true }}
          className="flex flex-wrap justify-center gap-6 mt-16"
        >
          {["SOC 2 Type II", "GDPR Compliant", "ISO 27001", "HTTPS Only"].map((badge, i) => (
            <div
              key={i}
              className="px-6 py-3 bg-white/5 border border-white/10 rounded-full text-gray-300 font-semibold"
            >
              {badge}
            </div>
          ))}
        </motion.div>
      </div>
    </section>
  );
}
