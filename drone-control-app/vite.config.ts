import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'

// https://vitejs.dev/config/
export default defineConfig({
  plugins: [react()],
  server: {
    port: parseInt(process.env.PORT || '5173'),
    host: true,
    strictPort: !!process.env.PORT,
    // Proxy API requests to the backend server
    proxy: {
      '/api': {
        target: process.env.services__api__https__0 || 'http://localhost:5000',
        changeOrigin: true,
        secure: false
      },
      '/droneHub': {
        target: process.env.services__api__https__0 || 'http://localhost:5000',
        changeOrigin: true,
        ws: true  // WebSocket support for SignalR
      }
    }
  },
  build: {
    outDir: 'dist',
    sourcemap: true
  }
})