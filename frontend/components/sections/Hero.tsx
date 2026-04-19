'use client';

import { ArrowRight, Activity, Sparkles, Zap, Shield, Server } from 'lucide-react';
import {
  motion,
  useScroll,
  useTransform,
  useMotionTemplate,
  useMotionValue,
} from 'framer-motion';
import { useRef, useEffect, useState } from 'react';

// Animated particle component
function Particle({ x, y, delay }: { x: number; y: number; delay: number }) {
  return (
    <motion.div
      initial={{ opacity: 0, scale: 0 }}
      animate={{ opacity: [0, 1, 0], scale: [0, 1, 0] }}
      transition={{
        duration: 3,
        repeat: Infinity,
        delay,
        ease: 'easeInOut',
      }}
      className='absolute w-1 h-1 bg-blue-400 rounded-full'
      style={{ left: `${x}%`, top: `${y}%` }}
    />
  );
}

// Interactive cursor glow
function CursorGlow() {
  const mouseX = useMotionValue(0);
  const mouseY = useMotionValue(0);

  useEffect(() => {
    const handleMouseMove = (e: MouseEvent) => {
      mouseX.set(e.clientX);
      mouseY.set(e.clientY);
    };
    window.addEventListener('mousemove', handleMouseMove);
    return () => window.removeEventListener('mousemove', handleMouseMove);
  }, [mouseX, mouseY]);

  const maskImage = useMotionTemplate`radial-gradient(400px at ${mouseX}px ${mouseY}px, white, transparent)`;

  return (
    <motion.div
      className='pointer-events-none fixed inset-0 z-50 transition duration-300'
      style={{ maskImage, WebkitMaskImage: maskImage }}
    >
      <div className='absolute inset-0 bg-gradient-to-r from-blue-500/10 via-purple-500/10 to-pink-500/10' />
    </motion.div>
  );
}

export default function Hero() {
  const containerRef = useRef<HTMLDivElement>(null);
  const { scrollYProgress } = useScroll({
    target: containerRef,
    offset: ['start start', 'end start'],
  });

  const y1 = useTransform(scrollYProgress, [0, 1], [0, 200]);
  const opacity = useTransform(scrollYProgress, [0, 0.5], [1, 0]);

  const [particles, setParticles] = useState<{ x: number; y: number; delay: number }[]>([]);

  useEffect(() => {
    const newParticles = Array.from({ length: 30 }, () => ({
      x: Math.random() * 100,
      y: Math.random() * 100,
      delay: Math.random() * 3,
    }));
    setParticles(newParticles);
  }, []);

  return (
    <section
      ref={containerRef}
      className='relative min-h-screen flex items-center justify-center overflow-hidden'
    >
      <CursorGlow />

      {/* Animated background gradient with multiple layers */}
      <motion.div style={{ opacity }} className='absolute inset-0'>
        <motion.div
          animate={{
            backgroundPosition: ['0% 0%', '100% 100%', '0% 0%'],
          }}
          transition={{
            duration: 20,
            repeat: Infinity,
            ease: 'linear',
          }}
          className='absolute inset-0 bg-gradient-to-br from-blue-600/30 via-purple-600/30 to-pink-600/30 opacity-60'
          style={{
            backgroundSize: '400% 400%',
          }}
        />
        <motion.div
          animate={{
            scale: [1, 1.2, 1],
            rotate: [0, 90, 0],
          }}
          transition={{
            duration: 15,
            repeat: Infinity,
            ease: 'easeInOut',
          }}
          className='absolute inset-0 bg-[radial-gradient(ellipse_at_center,_var(--tw-gradient-stops))] from-blue-900/40 via-purple-900/30 to-transparent'
        />
      </motion.div>

      {/* Animated grid pattern */}
      <motion.div
        style={{ y: y1 }}
        className='absolute inset-0 bg-[linear-gradient(to_right,#80808012_1px,transparent_1px),linear-gradient(to_bottom,#80808012_1px,transparent_1px)] bg-[size:32px_32px]'
      />

      {/* Floating particles */}
      <div className='absolute inset-0 pointer-events-none'>
        {particles.map((particle, i) => (
          <Particle key={i} {...particle} />
        ))}
      </div>

      {/* Animated orbs */}
      <motion.div
        animate={{
          x: [0, 100, 0],
          y: [0, -100, 0],
        }}
        transition={{
          duration: 20,
          repeat: Infinity,
          ease: 'easeInOut',
        }}
        className='absolute top-1/4 left-1/4 w-96 h-96 bg-blue-500/20 rounded-full blur-3xl'
      />
      <motion.div
        animate={{
          x: [0, -100, 0],
          y: [0, 100, 0],
        }}
        transition={{
          duration: 25,
          repeat: Infinity,
          ease: 'easeInOut',
        }}
        className='absolute bottom-1/4 right-1/4 w-96 h-96 bg-purple-500/20 rounded-full blur-3xl'
      />

      <div className='container mx-auto px-6 relative z-10'>
        <motion.div
          initial={{ opacity: 0 }}
          animate={{ opacity: 1 }}
          transition={{ duration: 0.8 }}
          className='text-center max-w-6xl mx-auto'
        >
          {/* Badge with glow */}
          <motion.div
            initial={{ opacity: 0, y: 30 }}
            animate={{ opacity: 1, y: 0 }}
            transition={{ delay: 0.2, duration: 0.6 }}
            className='inline-flex items-center gap-3 px-6 py-3 rounded-full bg-gradient-to-r from-white/10 to-white/5 backdrop-blur-xl border border-white/20 mb-10 group hover:border-blue-400/50 transition-all duration-300'
          >
            <motion.div
              animate={{ rotate: 360 }}
              transition={{ duration: 10, repeat: Infinity, ease: 'linear' }}
              className='relative'
            >
              <Activity className='w-5 h-5 text-blue-400' />
              <div className='absolute inset-0 bg-blue-400 blur-xl opacity-50' />
            </motion.div>
            <span className='text-sm font-medium text-gray-200'>Real-time Server Monitoring</span>
            <Sparkles className='w-4 h-4 text-purple-400' />
          </motion.div>

          {/* Main heading with staggered animation */}
          <motion.h1
            initial={{ opacity: 0, y: 30 }}
            animate={{ opacity: 1, y: 0 }}
            transition={{ delay: 0.3, duration: 0.6 }}
            className='text-6xl md:text-7xl lg:text-9xl font-bold mb-8 leading-tight'
          >
            <motion.span
              className='bg-clip-text text-transparent bg-gradient-to-r from-white via-blue-100 to-purple-100'
              animate={{
                backgroundPosition: ['0% 50%', '100% 50%', '0% 50%'],
              }}
              transition={{
                duration: 5,
                repeat: Infinity,
                ease: 'easeInOut',
              }}
              style={{ backgroundSize: '200% auto' }}
            >
              Monitor Your
            </motion.span>
            <br />
            <motion.span
              className='bg-clip-text text-transparent bg-gradient-to-r from-blue-400 via-purple-400 to-pink-400'
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
              Servers Like Never
            </motion.span>
            <br />
            <motion.span
              className='bg-clip-text text-transparent bg-gradient-to-r from-pink-400 via-purple-400 to-blue-400'
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
              Before
            </motion.span>
          </motion.h1>

          {/* Description with typing effect */}
          <motion.p
            initial={{ opacity: 0, y: 30 }}
            animate={{ opacity: 1, y: 0 }}
            transition={{ delay: 0.4, duration: 0.6 }}
            className='text-xl md:text-2xl lg:text-3xl text-gray-300 mb-14 max-w-4xl mx-auto leading-relaxed font-light'
          >
            Real-time metrics, instant alerts, and powerful insights.
            <br />
            <span className='text-white font-medium'>Keep your infrastructure healthy</span> with
            ServerEye.
          </motion.p>

          {/* CTA Buttons with 3D hover effect */}
          <motion.div
            initial={{ opacity: 0, y: 30 }}
            animate={{ opacity: 1, y: 0 }}
            transition={{ delay: 0.5, duration: 0.6 }}
            className='flex flex-col sm:flex-row gap-5 justify-center items-center'
          >
            <motion.button
              whileHover={{ scale: 1.05, rotateX: 5 }}
              whileTap={{ scale: 0.95 }}
              className='group relative px-10 py-5 bg-gradient-to-r from-blue-600 to-purple-600 rounded-full font-semibold text-xl transition-all duration-300 flex items-center gap-3 shadow-2xl shadow-blue-500/50 overflow-hidden'
            >
              <motion.div className='absolute inset-0 bg-gradient-to-r from-blue-500 to-purple-500 opacity-0 group-hover:opacity-100 transition-opacity' />
              <span className='relative z-10 flex items-center gap-3'>
                Try Free
                <ArrowRight className='w-6 h-6 group-hover:translate-x-2 transition-transform' />
              </span>
              <motion.div className='absolute inset-0 bg-white/20 translate-y-full group-hover:translate-y-0 transition-transform duration-300' />
            </motion.button>
            <motion.button
              whileHover={{ scale: 1.05 }}
              whileTap={{ scale: 0.95 }}
              className='px-10 py-5 bg-white/5 hover:bg-white/10 backdrop-blur-xl border border-white/20 rounded-full font-semibold text-xl transition-all duration-300 flex items-center gap-3 hover:border-purple-400/50 group'
            >
              <Zap className='w-5 h-5 text-purple-400 group-hover:scale-110 transition-transform' />
              View Demo
            </motion.button>
          </motion.div>

          {/* Stats with animated counters */}
          <motion.div
            initial={{ opacity: 0, y: 30 }}
            animate={{ opacity: 1, y: 0 }}
            transition={{ delay: 0.6, duration: 0.6 }}
            className='mt-24 grid grid-cols-3 gap-8 max-w-3xl mx-auto'
          >
            {[
              { value: '10K+', label: 'Servers Monitored', icon: Server },
              { value: '99.9%', label: 'Uptime', icon: Shield },
              { value: '<1s', label: 'Response Time', icon: Zap },
            ].map((stat, i) => (
              <motion.div key={i} whileHover={{ scale: 1.1, y: -5 }} className='text-center group'>
                <motion.div
                  className='text-4xl md:text-5xl font-bold bg-clip-text text-transparent bg-gradient-to-r from-blue-400 to-purple-400 mb-2'
                  animate={{
                    backgroundPosition: ['0% 50%', '100% 50%', '0% 50%'],
                  }}
                  transition={{
                    duration: 3,
                    repeat: Infinity,
                    ease: 'easeInOut',
                    delay: i * 0.2,
                  }}
                  style={{ backgroundSize: '200% auto' }}
                >
                  {stat.value}
                </motion.div>
                <div className='text-sm text-gray-400 group-hover:text-gray-300 transition-colors'>
                  {stat.label}
                </div>
              </motion.div>
            ))}
          </motion.div>
        </motion.div>
      </div>

      {/* Scroll indicator with enhanced animation */}
      <motion.div
        style={{ opacity }}
        className='absolute bottom-10 left-1/2 transform -translate-x-1/2'
      >
        <motion.div
          animate={{ y: [0, 10, 0] }}
          transition={{ duration: 2, repeat: Infinity, ease: 'easeInOut' }}
          className='w-8 h-12 border-2 border-white/40 rounded-full flex justify-center backdrop-blur-sm'
        >
          <motion.div
            animate={{ y: [0, 16, 0], opacity: [1, 0.5, 1] }}
            transition={{ duration: 2, repeat: Infinity, ease: 'easeInOut' }}
            className='w-2 h-2 bg-gradient-to-b from-blue-400 to-purple-400 rounded-full mt-2'
          />
        </motion.div>
      </motion.div>
    </section>
  );
}
