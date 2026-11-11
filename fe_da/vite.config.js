import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'

// https://vite.dev/config/
export default defineConfig({
  plugins: [react()],
  server: {
    port: 5173,
    proxy: {
      '/api': {
        target: 'https://localhost:7207',
        changeOrigin: true,
<<<<<<< HEAD
        secure: true,
=======
        secure: false,
        //secure: true,
>>>>>>> 0d40ca35efaf07c5d4887d6f8febc66c57f5578c
      },
      '/signin-google': {
        target: 'https://localhost:7207',
        changeOrigin: true,
<<<<<<< HEAD
        secure: true,
=======
        secure: false,
        //secure: true,
>>>>>>> 0d40ca35efaf07c5d4887d6f8febc66c57f5578c
      }
    }
  }
})
