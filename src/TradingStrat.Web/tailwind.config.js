/** @type {import('tailwindcss').Config} */
module.exports = {
  content: [
    "./Components/**/*.{razor,html,cshtml}",
    "./Pages/**/*.{razor,html,cshtml}"
  ],
  darkMode: 'class',
  theme: {
    extend: {
      colors: {
        // Light theme colors (existing)
        'trading-green': '#10b981',
        'trading-red': '#ef4444',
        'trading-blue': '#3b82f6',

        // Enhanced dark theme (matching reference screenshots)
        'dark-bg': '#060913',        // Darker background
        'dark-card': '#0f1419',      // Very dark cards
        'dark-elevated': '#1a1f2e',  // Elevated surfaces
        'dark-border': '#1e2530',    // Subtle borders
        'dark-text-primary': '#f0f4f8',   // Brighter text
        'dark-text-secondary': '#94a3b8',  // Muted text
        'dark-accent-blue': '#3b82f6',     // Vivid blue
        'dark-success': '#10b981',   // Vibrant green
        'dark-danger': '#ef4444',    // Vibrant red
        'dark-info': '#06b6d4',      // Cyan
        'dark-warning': '#f59e0b',   // Amber
      },
    },
  },
  plugins: [
    require('@tailwindcss/forms'),
    require('@tailwindcss/typography'),
  ],
}
