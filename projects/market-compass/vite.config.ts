import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'
import tailwindcss from '@tailwindcss/vite'
import path from 'path'

// Browser CORS blocks direct calls to the PSX Data Portal and Yahoo Finance.
// In dev we proxy through the Vite server; in production the same /api/* paths are
// served by the serverless functions in /api (see api/psx & api/yahoo).
const UA =
  'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36'

export default defineConfig(({ command }) => ({
  base: command === 'build' ? '/HFJ-Cognitive-Lab/market-compass/' : '/',
  plugins: [react(), tailwindcss()],
  resolve: {
    alias: {
      '@': path.resolve(__dirname, './src'),
    },
  },
  server: {
    proxy: {
      '/api/psx': {
        target: 'https://dps.psx.com.pk',
        changeOrigin: true,
        secure: true,
        headers: { 'User-Agent': UA },
        rewrite: (p) => p.replace(/^\/api\/psx/, ''),
      },
      '/api/yahoo': {
        target: 'https://query1.finance.yahoo.com',
        changeOrigin: true,
        secure: true,
        headers: { 'User-Agent': UA },
        rewrite: (p) => p.replace(/^\/api\/yahoo/, ''),
      },
    },
  },
}))
