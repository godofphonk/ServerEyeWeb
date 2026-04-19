'use client';

import { motion } from 'framer-motion';
import { Star, Quote } from 'lucide-react';
import { useState } from 'react';

const testimonials = [
  {
    name: 'Alex Johnson',
    role: 'DevOps Engineer',
    rating: 5,
    text: 'ServerEye saved us countless hours. Setup took literally 2 minutes and we were monitoring 50+ servers instantly.',
  },
  {
    name: 'Sarah Chen',
    role: 'CTO at TechStartup',
    rating: 5,
    text: "Finally, a monitoring tool that doesn't cost an arm and a leg. Open source and feature-rich!",
  },
  {
    name: 'Mike Rodriguez',
    role: 'SRE Lead',
    rating: 5,
    text: 'The real-time alerts via Telegram are a game-changer. I know about issues before my users do.',
  },
  {
    name: 'Emma Williams',
    role: 'Infrastructure Manager',
    rating: 4,
    text: 'Love the simplicity. No bloated dashboards, just the metrics that matter.',
  },
  {
    name: 'David Kim',
    role: 'Full Stack Developer',
    rating: 5,
    text: 'Open source, secure, and incredibly easy to use. This is how monitoring should be done.',
  },
  {
    name: 'Lisa Anderson',
    role: 'System Administrator',
    rating: 5,
    text: 'We switched from Datadog and saved $3000/month. ServerEye does everything we need.',
  },
];

export default function Testimonials() {
  const [isPaused, setIsPaused] = useState(false);
  const allTestimonials = [...testimonials, ...testimonials];

  return (
    <section className='py-32 relative overflow-hidden'>
      <div className='absolute inset-0 bg-gradient-to-b from-transparent via-yellow-900/5 to-transparent' />

      <div className='container mx-auto px-6 relative z-10'>
        <motion.div
          initial={{ opacity: 0, y: 30 }}
          whileInView={{ opacity: 1, y: 0 }}
          viewport={{ once: true }}
          transition={{ duration: 0.8 }}
          className='text-center mb-20'
        >
          <h2 className='text-5xl md:text-6xl lg:text-7xl font-bold mb-6'>
            Loved By
            <motion.span
              className='bg-clip-text text-transparent bg-gradient-to-r from-yellow-400 via-orange-400 to-red-400'
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
              Thousands
            </motion.span>
          </h2>
          <p className='text-xl text-gray-400 max-w-2xl mx-auto'>
            See what developers and teams are saying about ServerEye
          </p>
        </motion.div>

        <div
          className='relative'
          onMouseEnter={() => setIsPaused(true)}
          onMouseLeave={() => setIsPaused(false)}
        >
          <div className='overflow-hidden'>
            <motion.div
              className='flex gap-6'
              animate={{
                x: isPaused ? undefined : [0, -1920],
              }}
              transition={{
                duration: 40,
                repeat: Infinity,
                ease: 'linear',
              }}
            >
              {allTestimonials.map((testimonial, i) => (
                <motion.div
                  key={i}
                  whileHover={{ scale: 1.05, y: -10 }}
                  transition={{ duration: 0.3 }}
                  className='flex-shrink-0 w-96 bg-gray-900/50 border border-white/10 rounded-2xl p-8 hover:border-yellow-500/50 transition-all duration-300'
                >
                  <div className='flex gap-1 mb-4'>
                    {[...Array(5)].map((_, starIndex) => (
                      <motion.div
                        key={starIndex}
                        initial={{ scale: 0 }}
                        animate={{ scale: 1 }}
                        transition={{ delay: starIndex * 0.1 }}
                      >
                        <Star
                          className={`w-5 h-5 ${
                            starIndex < testimonial.rating
                              ? 'text-yellow-400 fill-yellow-400'
                              : 'text-gray-600'
                          }`}
                        />
                      </motion.div>
                    ))}
                  </div>

                  <div className='relative mb-6'>
                    <Quote className='w-8 h-8 text-yellow-400/20 absolute -top-4 -left-2' />
                    <p className='text-gray-300 leading-relaxed relative z-10'>
                      "{testimonial.text}"
                    </p>
                  </div>

                  <div className='flex items-center gap-4'>
                    <motion.div
                      whileHover={{ scale: 1.1, rotate: 5 }}
                      className='w-12 h-12 bg-gradient-to-br from-yellow-500 to-orange-500 rounded-full flex items-center justify-center text-white font-bold text-lg shadow-lg shadow-yellow-500/30'
                    >
                      {testimonial.name[0]}
                    </motion.div>
                    <div>
                      <div className='font-semibold'>{testimonial.name}</div>
                      <div className='text-sm text-gray-500'>{testimonial.role}</div>
                    </div>
                  </div>
                </motion.div>
              ))}
            </motion.div>
          </div>

          <div className='absolute inset-y-0 left-0 w-32 bg-gradient-to-r from-black to-transparent pointer-events-none' />
          <div className='absolute inset-y-0 right-0 w-32 bg-gradient-to-l from-black to-transparent pointer-events-none' />
        </div>

        <motion.div
          initial={{ opacity: 0, y: 30 }}
          whileInView={{ opacity: 1, y: 0 }}
          viewport={{ once: true }}
          transition={{ delay: 0.6 }}
          className='text-center mt-16'
        >
          <motion.div
            whileHover={{ scale: 1.05 }}
            className='inline-flex items-center gap-3 px-8 py-4 bg-yellow-500/10 border border-yellow-500/20 rounded-full hover:bg-yellow-500/20 transition-all'
          >
            <motion.div
              animate={{ rotate: [0, 10, -10, 0] }}
              transition={{ duration: 2, repeat: Infinity, ease: 'easeInOut' }}
            >
              <Star className='w-6 h-6 text-yellow-400 fill-yellow-400' />
            </motion.div>
            <span className='text-xl font-bold text-white'>4.9/5</span>
            <span className='text-gray-400'>from 500+ reviews</span>
          </motion.div>
        </motion.div>
      </div>
    </section>
  );
}
