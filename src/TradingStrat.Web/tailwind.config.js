/** @type {import('tailwindcss').Config} */
module.exports = {
  content: [
    "./Components/**/*.{razor,html,cshtml}",
    "./Pages/**/*.{razor,html,cshtml}"
  ],
  theme: {
    extend: {
      colors: {
        'trading-green': '#10b981',
        'trading-red': '#ef4444',
        'trading-blue': '#3b82f6',
      },
    },
  },
  plugins: [
    require('@tailwindcss/forms'),
    require('@tailwindcss/typography'),
  ],
}
