import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'
import tailwindcss from '@tailwindcss/vite'

// https://vite.dev/config/
export default defineConfig({
  plugins: [react(), tailwindcss()],
  server: {
    proxy: {
      // Cible l'endpoint https (pas http) : Bff:Origin — utilisé par ApiService pour fixer son
      // propre redirect_uri OIDC et par IdentityService pour enregistrer le client — pointe vers
      // cet endpoint https. Les cookies posés pendant le flow OIDC (SameSite=None) exigent de
      // toute façon une connexion https pour survivre. "secure: false" désactive la vérification
      // du certificat côté proxy Node (le certificat de dev ASP.NET Core est fiable pour le
      // système mais pas pour le magasin de confiance de Node).
      '/api': {
        target: process.env.services__apiservice__https__0 ?? 'https://localhost:5050',
        changeOrigin: true,
        secure: false,
      },
      // Endpoints de gestion de session Duende.BFF (/bff/login, /bff/user, /bff/logout...),
      // servis par le même ApiService — doivent passer par le même proxy que /api pour que le
      // cookie de session soit posé sur l'origine du SPA (localhost:5173), pas sur celle du
      // backend.
      '/bff': {
        target: process.env.services__apiservice__https__0 ?? 'https://localhost:5050',
        changeOrigin: true,
        secure: false,
      },
    },
  },
})
