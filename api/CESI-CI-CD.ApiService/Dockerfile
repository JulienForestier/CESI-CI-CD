# Stage 1 : Build
FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine AS builder

WORKDIR /src

# Copier le code source et publier (restore + publish en une seule passe)
COPY . .
RUN dotnet publish CESI-CI-CD.ApiService.csproj -c Release -o /app/publish

# Stage 2 : Servir
FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine

WORKDIR /app

# Copier les artefacts publiés
COPY --from=builder /app/publish .

# Port d'exposition
EXPOSE 8080

# Commande de démarrage
CMD ["dotnet", "CESI-CI-CD.ApiService.dll"]
