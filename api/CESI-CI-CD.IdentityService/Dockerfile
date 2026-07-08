# Stage 1 : Build
FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine AS builder

WORKDIR /src

# wwwroot est attendu déjà peuplé (build de application/identity-ui copié en amont par la CI —
# voir ci-identity.yml/deploy.yml) : le contexte de ce build reste volontairement restreint au
# dossier du projet (comme CESI-CI-CD.ApiService/Dockerfile), donc pas de build Node ici.
COPY . .
RUN dotnet publish CESI-CI-CD.IdentityService.csproj -c Release -o /app/publish

# Stage 2 : Servir
FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine

WORKDIR /app

# Copier les artefacts publiés
COPY --from=builder /app/publish .

# Exécuter en non-root (utilisateur "app" fourni par l'image de base .NET)
USER $APP_UID

# Port d'exposition
EXPOSE 8080

# Commande de démarrage
CMD ["dotnet", "CESI-CI-CD.IdentityService.dll"]
