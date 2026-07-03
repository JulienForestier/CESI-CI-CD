# Stage 1 : Build
FROM node:22-alpine AS builder

WORKDIR /app

# Copier package files
COPY package*.json ./

# Installer les dépendances
RUN npm ci

# Copier le code source
COPY . .

# Construire l'application
RUN npm run build

# Stage 2 : Servir
FROM node:22-alpine

WORKDIR /app

# Installer un serveur HTTP léger
RUN npm install -g serve

# Copier les artefacts du build
COPY --from=builder /app/dist ./dist

# Port d'exposition
EXPOSE 3000

# Commande de démarrage
CMD ["serve", "-s", "dist", "-l", "3000"]