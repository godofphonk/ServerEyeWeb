'use client';

import { motion } from 'framer-motion';
import { Star } from 'lucide-react';
import { useEffect, useState } from 'react';

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

  // Duplicate testimonials for seamless loop
  const allTestimonials = [...testimonials, ...testimonials];

  return (
    <section className='py-32 relative overflow-hidden'>
      <div className='container mx-auto px-6'>
        <motion.div
          initial={{ opacity: 0, y: 20 }}
          whileInView={{ opacity: 1, y: 0 }}
          viewport={{ once: true }}
          className='text-center mb-20'
        >
          <h2 className='text-5xl md:text-6xl font-bold mb-6'>
            Loved By
            <span className='bg-clip-text text-transparent bg-gradient-to-r from-yellow-400 to-orange-400'>
              {' '}
              Thousands
            </span>
          </h2>
          <p className='text-xl text-gray-400 max-w-2xl mx-auto'>
            See what developers and teams are saying about ServerEye
          </p>
        </motion.div>

        {/* Scrolling testimonials */}
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
                <div
                  key={i}
                  className='flex-shrink-0 w-96 bg-gradient-to-br from-gray-900 to-black border border-white/10 rounded-2xl p-8'
                >
                  {/* Stars */}
                  <div className='flex gap-1 mb-4'>
                    {[...Array(5)].map((_, starIndex) => (
                      <Star
                        key={starIndex}
                        className={`w-5 h-5 ${
                          starIndex < testimonial.rating
                            ? 'text-yellow-400 fill-yellow-400'
                            : 'text-gray-600'
                        }`}
                      />
                    ))}
                  </div>

                  {/* Text */}
                  <p className='text-gray-300 mb-6 leading-relaxed'>"{testimonial.text}"</p>

                  {/* Author */}
                  <div className='flex items-center gap-4'>
                    <div className='w-12 h-12 bg-gradient-to-br from-blue-600 to-purple-600 rounded-full flex items-center justify-center text-white font-bold text-lg'>
                      {testimonial.name[0]}
                    </div>
                    <div>
                      <div className='font-semibold'>{testimonial.name}</div>
                      <div className='text-sm text-gray-500'>{testimonial.role}</div>
                    </div>
                  </div>
                </div>
              ))}
            </motion.div>
          </div>

          {/* Gradient overlays */}
          <div className='absolute inset-y-0 left-0 w-32 bg-gradient-to-r from-black to-transparent pointer-events-none' />
          <div className='absolute inset-y-0 right-0 w-32 bg-gradient-to-l from-black to-transparent pointer-events-none' />
        </div>

        {/* Average rating */}
        <motion.div
          initial={{ opacity: 0 }}
          whileInView={{ opacity: 1 }}
          viewport={{ once: true }}
          className='text-center mt-16'
        >
          <div className='inline-flex items-center gap-2 px-6 py-3 bg-yellow-500/10 border border-yellow-500/20 rounded-full'>
            <Star className='w-5 h-5 text-yellow-400 fill-yellow-400' />
            <span className='text-xl font-bold text-white'>4.9/5</span>
            <span className='text-gray-400'>from 500+ reviews</span>
          </div>
        </motion.div>
      </div>
    </section>
  );
}
