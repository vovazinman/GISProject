/**
 * Application Configuration
 * Centralized configuration from environment variables
 */

export const config = {
  // API settings
  api: {
    baseUrl: import.meta.env.VITE_API_BASE_URL || 'http://localhost:5000',
    endpoints: {
      drone: '/api/drone',
      chat: '/api/chat',
      mission: '/api/mission'
    }
  },

  // SignalR settings
  signalR: {
    hubUrl: import.meta.env.VITE_SIGNALR_HUB_URL || 'http://localhost:5000/droneHub'
  },

  // App settings
  app: {
    name: import.meta.env.VITE_APP_NAME || 'Drone Control Center'
  }
} as const;

export type Config = typeof config;
export default config;