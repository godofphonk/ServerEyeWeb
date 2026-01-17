"use client";

import { motion } from "framer-motion";
import { Server, Cpu, HardDrive, Activity, Zap } from "lucide-react";

export default function Visualization() {
  return (
    <section className="py-32 relative overflow-hidden">
      <div className="container mx-auto px-6">
        <motion.div
          initial={{ opacity: 0, y: 20 }}
          whileInView={{ opacity: 1, y: 0 }}
          viewport={{ once: true }}
          transition={{ duration: 0.6 }}
          className="text-center mb-16"
        >
          <h2 className="text-5xl md:text-6xl font-bold mb-6">
            See Everything
            <span className="bg-clip-text text-transparent bg-gradient-to-r from-blue-400 to-purple-400">
              {" "}In Real-Time
            </span>
          </h2>
          <p className="text-xl text-gray-400 max-w-2xl mx-auto">
            Beautiful dashboards with live metrics, charts, and alerts
          </p>
        </motion.div>

        {/* Mock Dashboard Preview */}
        <motion.div
          initial={{ opacity: 0, scale: 0.95 }}
          whileInView={{ opacity: 1, scale: 1 }}
          viewport={{ once: true }}
          transition={{ duration: 0.8 }}
          className="relative max-w-6xl mx-auto"
        >
          {/* Glow effect */}
          <div className="absolute inset-0 bg-gradient-to-r from-blue-600/20 to-purple-600/20 blur-3xl" />
          
          {/* Dashboard mockup */}
          <div className="relative bg-gradient-to-br from-gray-900 to-black border border-white/10 rounded-2xl p-8 backdrop-blur-sm">
            {/* Header */}
            <div className="flex items-center justify-between mb-8">
              <div>
                <h3 className="text-2xl font-bold">Server Dashboard</h3>
                <p className="text-gray-500">Real-time monitoring</p>
              </div>
              <div className="flex items-center gap-2">
                <div className="w-3 h-3 bg-green-500 rounded-full animate-pulse" />
                <span className="text-sm text-gray-400">Live</span>
              </div>
            </div>

            {/* Metrics Grid */}
            <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-6 mb-8">
              {[
                { icon: Cpu, label: "CPU Usage", value: "45%", color: "blue" },
                { icon: HardDrive, label: "Memory", value: "8.2 GB", color: "purple" },
                { icon: Server, label: "Disk Usage", value: "67%", color: "pink" },
                { icon: Activity, label: "Network", value: "1.2 GB/s", color: "green" },
              ].map((metric, i) => (
                <motion.div
                  key={i}
                  initial={{ opacity: 0, y: 20 }}
                  whileInView={{ opacity: 1, y: 0 }}
                  viewport={{ once: true }}
                  transition={{ delay: i * 0.1 }}
                  className="bg-white/5 border border-white/10 rounded-xl p-6 hover:bg-white/10 transition-all duration-300"
                >
                  <metric.icon className={`w-8 h-8 text-${metric.color}-400 mb-4`} />
                  <div className="text-3xl font-bold mb-2">{metric.value}</div>
                  <div className="text-sm text-gray-400">{metric.label}</div>
                </motion.div>
              ))}
            </div>

            {/* Chart placeholder */}
            <div className="bg-white/5 border border-white/10 rounded-xl p-6 h-64 flex items-center justify-center">
              <div className="text-center">
                <Zap className="w-16 h-16 text-blue-400 mx-auto mb-4" />
                <p className="text-gray-400">Live metrics chart</p>
              </div>
            </div>
          </div>
        </motion.div>
      </div>
    </section>
  );
}
