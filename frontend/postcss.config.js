module.exports = {
  plugins: [
    require('tailwindcss'),
    require('autoprefixer'),
    // PurgeCSS for production
    ...(process.env.NODE_ENV === 'production'
      ? [
          require('@fullhuman/postcss-purgecss')({
            content: [
              './pages/**/*.{js,jsx,ts,tsx}',
              './components/**/*.{js,jsx,ts,tsx}',
              './app/**/*.{js,jsx,ts,tsx}',
            ],
            defaultExtractor: (content) => {
              const broadMatches = content.match(/[^<>"'`\s]*[^<>"'`\s:]/g) || [];
              const innerMatches = content.match(/[^<>"'`\s.()]*[^<>"'`\s.():]/g) || [];
              return broadMatches.concat(innerMatches);
            },
            safelist: [
              // Add any dynamic classes that should not be purged
              /^bg-/,
              /^text-/,
              /^border-/,
              /^hover:/,
              /^focus:/,
              /^active:/,
              /^disabled:/,
              /^group-hover:/,
            ],
          }),
        ]
      : []),
    // CSS nano for minification in production
    ...(process.env.NODE_ENV === 'production' ? [require('cssnano')({ preset: 'default' })] : []),
  ],
};
