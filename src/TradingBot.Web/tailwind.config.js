/** @type {import('tailwindcss').Config} */
module.exports = {
  darkMode: 'class', // Enable class-based dark mode
  content: [
    './Components/**/*.{razor,html,cshtml}',
    './Pages/**/*.{razor,html,cshtml}'
  ],
  theme: {
    extend: {
      colors: {
        // Custom color palette using CSS variables
        background: 'rgb(var(--color-background) / <alpha-value>)',
        foreground: 'rgb(var(--color-foreground) / <alpha-value>)',
        primary: 'rgb(var(--color-primary) / <alpha-value>)',
        success: 'rgb(var(--color-success) / <alpha-value>)',
        warning: 'rgb(var(--color-warning) / <alpha-value>)',
        danger: 'rgb(var(--color-danger) / <alpha-value>)',
      },
      // Desktop-first responsive design with lg (1024px) as minimum
      screens: {
        'lg': '1024px',   // Minimum supported width
        'xl': '1280px',   // Standard desktop
        '2xl': '1536px',  // Large desktop
      },
      // Smooth transitions for theme switching
      transitionProperty: {
        'height': 'height',
        'spacing': 'margin, padding',
      }
    },
  },
  plugins: [],
}
