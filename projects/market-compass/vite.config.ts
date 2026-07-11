import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'
import tailwindcss from '@tailwindcss/vite'
import path from 'path'
import { handleYahooProxy } from './api/yahoo/_yahooProxy.js'

// Browser CORS blocks direct calls to the PSX Data Portal and Yahoo Finance.
// In dev we proxy through the Vite server; in production the same /api/* paths are
// served by the serverless functions in /api (see api/psx & api/yahoo).
const UA =
  'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36'

// Dev-server middleware that runs the IDENTICAL shared Yahoo proxy logic used in
// production (crumb handshake, host-pin, allowlist, rate-limit) — NFR-5 parity.
function yahooDevProxy() {
  return {
    name: 'yahoo-dev-proxy',
    configureServer(server: { middlewares: { use: (fn: (req: any, res: any, next: () => void) => void) => void } }) {
      server.middlewares.use((req, res, next) => {
        if (req.url && req.url.startsWith('/api/yahoo')) {
          handleYahooProxy(req, res)
          return
        }
        next()
      })
    },
  }
}

export default defineConfig(() => ({
  base: '/',
  plugins: [react(), tailwindcss(), yahooDevProxy()],
  resolve: {
    alias: {
      '@': path.resolve(__dirname, './src'),
    },
  },
  server: {
    proxy: {
      // PSX stays a simple pass-through proxy; the fixed `target` pins the host
      // to dps.psx.com.pk (parity with the serverless host-pin).
      '/api/psx': {
        target: 'https://dps.psx.com.pk',
        changeOrigin: true,
        secure: true,
        headers: { 'User-Agent': UA },
        rewrite: (p) => p.replace(/^\/api\/psx/, ''),
      },
      // NOTE: /api/yahoo is intentionally NOT listed here — it is handled by the
      // yahooDevProxy() middleware above so dev runs the same crumb logic as prod.
    },
  },
}))
