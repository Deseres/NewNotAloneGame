import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'
import tailwindcss from '@tailwindcss/vite'

export default defineConfig({
  plugins: [
    react(),
    tailwindcss(),
  ],
  server: {
    proxy: {
      '/api': {
        target: 'https://notalone-api-bjbza7e9gafjfrfv.swedencentral-01.azurewebsites.net',
        changeOrigin: true,
        secure: false,
      }
    }
  }
})