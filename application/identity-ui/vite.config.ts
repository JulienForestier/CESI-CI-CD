import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'
import tailwindcss from '@tailwindcss/vite'

export default defineConfig({
  plugins: [react(), tailwindcss()],
  // IdentityService et le frontend principal sont servis sous le même host d'ingress (chemins
  // différents, pas de sous-domaine) — un base "/" par défaut ferait collisionner ce build avec
  // les assets hashés de collector-shop sous /assets/*.
  base: '/identity-assets/',
})
