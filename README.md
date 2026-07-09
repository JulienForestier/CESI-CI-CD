# Collector.shop

Marketplace C2C d'objets de collection (baskets en édition limitée, vinyles, figurines…), développée dans le cadre de l'évaluation du bloc CESI **« Superviser et assurer le développement des applications logicielles »**.

Ce dépôt contient l'**application** (backend .NET + frontends React) et sa **chaîne CI/CD**. Le déploiement est piloté en **GitOps** depuis un dépôt séparé : [`CESI-CI-CD-kubernetes`](https://github.com/JulienForestier/CESI-CI-CD-kubernetes).

## Architecture (en bref)

Trois services applicatifs indépendants, unifiés en production par un ingress Traefik (routage par chemin, même origine) :

| Service | Techno | Rôle |
|---|---|---|
| **myapp** (`application/collector-shop`) | React 19 + Vite | SPA : catalogue public + espace authentifié. Ne détient aucun token. |
| **apiservice** (`api/CESI-CI-CD.ApiService`) | .NET 10 + **Duende.BFF** | Backend-for-Frontend (client OAuth confidentiel, cookie de session) **et** API métier REST. |
| **identityservice** (`api/CESI-CI-CD.IdentityService`) | .NET 10 + **Duende IdentityServer** | Fournisseur OpenID Connect (Authorization Code + PKCE) + sa propre UI de login (`application/identity-ui`). |

Authentification **OAuth 2.0 / OIDC** via le pattern **BFF** : les tokens restent côté serveur, le navigateur ne détient qu'un cookie de session `HttpOnly`. Détails et diagrammes : **[docs/livrables/05-architecture-technique.md](docs/livrables/05-architecture-technique.md)**.

## Prérequis

| Outil | Version | Pourquoi |
|---|---|---|
| **.NET SDK** | **10.0.x** | Backend + AppHost Aspire |
| **Node.js** | **22.x** (+ npm) | Frontends React (Vite) |
| **Docker** (ou Podman) | récent | Aspire démarre un conteneur **PostgreSQL** au lancement |

> L'orchestrateur local **.NET Aspire** est embarqué dans le projet (packages NuGet, Aspire SDK 13.4.x) — pas de workload à installer séparément. Le tableau de bord Aspire se lance avec l'AppHost.

## Lancer en local (Aspire)

L'AppHost (`api/CESI-CI-CD`) orchestre PostgreSQL, l'IdentityServer, le BFF/API et le SPA collector-shop, avec découverte de services et variables d'environnement câblées automatiquement.

```bash
# 1. Cloner
git clone https://github.com/JulienForestier/CESI-CI-CD.git
cd CESI-CI-CD

# 2. Fournir le secret client OAuth du BFF (paramètre Aspire, via user-secrets)
dotnet user-secrets set "Parameters:bff-client-secret" "dev-bff-secret-change-me" \
  --project api/CESI-CI-CD

# 3. (Une fois) builder l'UI de login dans le wwwroot de l'IdentityServer
#    — nécessaire pour que la page /login s'affiche en local (elle n'est pas
#    lancée par Aspire, contrairement au SPA collector-shop).
npm --prefix application/identity-ui ci
npm --prefix application/identity-ui run build
mkdir -p api/CESI-CI-CD.IdentityService/wwwroot
cp -r application/identity-ui/dist/. api/CESI-CI-CD.IdentityService/wwwroot/

# 4. Démarrer toute la stack (Docker doit tourner)
dotnet run --project api/CESI-CI-CD
```

Au démarrage, le **tableau de bord Aspire** s'ouvre (URL + token de connexion affichés dans la console, ex. `https://localhost:17279`). Il liste les endpoints de chaque ressource : ouvrir celui de la ressource **`app`** (le SPA collector-shop) pour utiliser l'application. Aspire gère Postgres, l'ordre de démarrage (`WaitFor`) et l'injection des URLs interservices.

> Le proxy Vite du SPA redirige `/api` et `/bff` vers le BFF (endpoint HTTPS découvert via Aspire), de sorte que le cookie de session soit posé sur l'origine du SPA. La première exécution télécharge l'image Postgres et restaure les packages — comptez quelques minutes.

## Tests, lint, build

```bash
# Backend (.NET) — 107 tests
dotnet test api/CESI-CI-CD.sln

# Frontend principal (collector-shop)
npm --prefix application/collector-shop ci
npm --prefix application/collector-shop run lint
npm --prefix application/collector-shop test        # vitest + couverture
npm --prefix application/collector-shop run build

# UI de login (identity-ui)
npm --prefix application/identity-ui ci
npm --prefix application/identity-ui test
```

## Structure du dépôt

```
api/
  CESI-CI-CD/                     AppHost Aspire (orchestration locale)
  CESI-CI-CD.ApiService/          BFF + API métier (.NET)
  CESI-CI-CD.IdentityService/     IdentityServer OIDC (.NET)
  CESI-CI-CD.ApiService.Tests/    tests d'intégration API (xUnit)
  CESI-CI-CD.IdentityService.Tests/
application/
  collector-shop/                SPA principale (React + Vite)
  identity-ui/                    UI de login/register (React + Vite)
.github/workflows/               pipeline CI/CD (voir ci-dessous)
docs/livrables/                  livrables pédagogiques (évaluation)
```

## CI/CD & déploiement

À chaque push / PR, l'orchestrateur **`.github/workflows/ci.yaml`** déclenche, selon les chemins modifiés, les pipelines front / api / identity, plus les scans de sécurité (Semgrep, CodeQL, Trivy, TruffleHog, OWASP Dependency-Check) et le **Quality Gate SonarCloud** (bloquant sur les PR). Après succès sur une branche déployable, **`deploy.yml`** build/signe (cosign) les 3 images, les pousse sur GHCR et met à jour le dépôt Kubernetes ; **ArgoCD** synchronise le cluster.

Mapping branche → environnement : `dev` → **dev**, `release/x.y.z` → **rec**, merge `release/* → main` → **prod**.

Détails : **[docs/livrables/03-cycle-vie-devsecops-pipeline.md](docs/livrables/03-cycle-vie-devsecops-pipeline.md)**.

## Livrables pédagogiques

L'ensemble des documents d'évaluation (indicateurs qualité, processus de test, cycle DevSecOps, architecture, backlog, expérimentation, plan de remédiation sécurité) est dans **[docs/livrables/](docs/livrables/README.md)**.
