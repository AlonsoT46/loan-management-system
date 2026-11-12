import tailwindcss from '@tailwindcss/vite'
import react from '@vitejs/plugin-react'
import { defineConfig } from 'vite'

// https://vite.dev/config/
export default defineConfig({
  plugins: [react(), tailwindcss()],
  server: {
    // While developing, Vite passes every /api request to the backend on this same computer.
    // The app only talks to Vite, so it also works from a phone on the same wifi (npm run dev -- --host),
    // and the browser never needs CORS (it is all the same address).
    proxy: {
      '/api': 'http://localhost:5201',
    },
  },
})
