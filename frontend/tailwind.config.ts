import type { Config } from 'tailwindcss';

// A cooperative/agrarian palette (deep forest + warm harvest accent)
// instead of generic SaaS blue, used consistently across all four
// role-scoped shells.
const config: Config = {
  content: ['./app/**/*.{ts,tsx}', './components/**/*.{ts,tsx}'],
  theme: {
    extend: {
      colors: {
        koperasi: {
          50: '#f2f7f3',
          100: '#dcebe0',
          200: '#b8d7c3',
          300: '#8fbc9e',
          400: '#5f9a76',
          500: '#3f7d59',
          600: '#2f6446',
          700: '#27503a',
          800: '#213f30',
          900: '#1b3428',
        },
        harvest: {
          400: '#e3a94b',
          500: '#d18f2c',
          600: '#b1741e',
          700: '#8f5d18',
          800: '#6f4714',
        },
      },
      fontFamily: {
        sans: ['ui-sans-serif', 'system-ui', 'Segoe UI', 'Inter', 'sans-serif'],
      },
    },
  },
  plugins: [],
};

export default config;
