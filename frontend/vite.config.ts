import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'

// In development, /api is proxied to the ASP.NET Core backend so the browser
// talks to a single origin (no CORS needed). Override the target with API_PROXY_TARGET.
export default defineConfig({
  plugins: [react()],
  server: {
    port: 5173,
    proxy: {
      '/api': {
        target: process.env.API_PROXY_TARGET ?? 'http://localhost:5080',
        changeOrigin: true,
      },
    },
  },
})
