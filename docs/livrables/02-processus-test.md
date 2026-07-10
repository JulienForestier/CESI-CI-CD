# Processus de test

## Types de tests, outils et parties prenantes

| Type de test | Objectif | Outil / Framework | Déclenchement | Partie prenante responsable |
|---|---|---|---|---|
| **Unitaire** (backend) | Valider la logique métier isolée (ex. règles de modération d'annonce, filtre anti-coordonnées, profil OIDC) | xUnit + coverlet | Chaque push / PR (`ci-api.yml`, `ci-identity.yml`) | Développeur backend |
| **Unitaire** (frontend) | Valider composants React, hooks, schémas Zod, client API (collector-shop **et** identity-ui) | Vitest + Testing Library + `@vitest/coverage-v8` | Chaque push / PR (`ci-front.yml`, `ci-identity.yml`) | Développeur frontend |
| **Intégration** (backend) | Valider un endpoint API de bout en bout (DB en mémoire, **cookie de session / flow OIDC réel**, HTTP réel) | `Microsoft.AspNetCore.Mvc.Testing` (`WebApplicationFactory`) + EF Core InMemory | Chaque push / PR (`ci-api.yml`, `ci-identity.yml`) | Développeur backend |
| **Acceptation** | Valider qu'une fonctionnalité respecte le backlog (critères d'acceptation) | Tests d'intégration ciblés sur les scénarios du backlog (ex. `PostListing_CreatesRejectedListing_WhenModerationFails`) — voir [Backlog](./06-backlog-fonctionnalite-metier.md) | Chaque push / PR | Développeur + Product Owner (revue manuelle en soutenance) |
| **Analyse statique de code (SAST)** | Détecter vulnérabilités et anti-patterns dans le code source **et les workflows CI** | Semgrep OSS, CodeQL (JS/TS + C#), SonarCloud (dont règles `githubactions`) | Chaque push / PR (`ci-security.yml`, `code-quality-sonar.yml`) | Security champion / Lead Dev |
| **Analyse de composants (SCA)** | Détecter les dépendances vulnérables (npm, NuGet) | OWASP Dependency-Check, `npm audit` / `dotnet list package --vulnerable` | Chaque push / PR | Security champion |
| **Scan de secrets** | Détecter des identifiants/clés commis par erreur | TruffleHog | Chaque push / PR | Security champion |
| **Scan de conteneurs** | Détecter les CVE dans les images Docker construites | Trivy | Chaque push / PR (avant déploiement) | DevOps / Security champion |
| **Scan d'infrastructure (IaC)** | Détecter les mauvaises pratiques Kubernetes | Kubescape | Chaque push / PR sur le dépôt Kubernetes | DevOps |
| **Charge** | Mesurer temps de réponse et taux d'échec sous concurrence | Siege | Automatique après chaque déploiement (`load-test.yml`), sur les 3 environnements | DevOps / Lead Dev |

Le pipeline applique donc **plus des deux types de tests minimum exigés**, avec au moins deux catégories exécutées automatiquement et bloquantes (unitaire + intégration côté fonctionnel, SAST + SCA côté sécurité).

## Preuve d'exécution réussie (dernière exécution en date)

- **Backend (.NET)** : **107 tests** — 89 côté `ApiService.Tests` (unitaires `ListingModerationServiceTests`, `ContactInfoFilterTests` + intégration `CatalogEndpointsTests`, `ChatEndpointsTests`, `ModerationEndpointsTests`, `FavoriteEndpointsTests`, `InterestEndpointsTests`, `NotificationEndpointsTests`, `UserEndpointsTests`) et 18 côté `IdentityService.Tests` (intégration `AuthEndpointsTests`, unité `CustomProfileServiceTests`, `HealthAndDiscoveryTests` vérifiant le discovery document OIDC) — **107/107 réussis**.
- **Front (React)** : **156 tests** — 127 côté `collector-shop` (composants, pages, hooks, contexte, schémas) + 29 côté `identity-ui` (formulaires login/register, garde-fous d'URL de retour), **156/156 réussis**, couverture ~98 % statements sur les deux applications.
- **SonarCloud Quality Gate** : `OK` sur les Pull Requests (couverture new code ≥ 80 %, seuil ; ratings A ; duplication < 3 %).
- **Sécurité** : Trivy, Semgrep, CodeQL (JS/TS + C#), TruffleHog, OWASP Dependency-Check — **tous passés** ; analyse `githubactions` de SonarCloud verte après durcissement des workflows.
- **Charge** (Siege, `dev`) : 913 transactions, **0 échec**, 0,49 s de temps de réponse moyen.

## Politique de blocage

- Le **Quality Gate SonarCloud** (couverture ≥ 80 % sur le code nouveau + notes A + duplication < 3 %) est une **règle de protection de branche GitHub** sur `dev` : une PR ne peut pas être fusionnée si le gate échoue, quel que soit l'avis du relecteur. L'analyse tourne aussi sur les push `dev`/`main` (en mode non-bloquant) pour maintenir le tableau de bord du projet à jour après chaque fusion.
- Les scans de sécurité (Trivy, Semgrep, CodeQL, TruffleHog, Dependency-Check, Kubescape) sont exécutés à chaque PR ; un échec bloque également la fusion via les *required status checks*.
- La détection de changements (`dorny/paths-filter`) n'exécute que les pipelines pertinents (`front` / `api` / `identity`), tandis que les scans de sécurité tournent systématiquement.
- Le test de charge (`load-test.yml`) se déclenche automatiquement après chaque déploiement réussi (`deploy.yml`), sur l'environnement réellement mis à jour (dev/rec/prod). Il reste **volontairement non bloquant** : les résultats sont publiés dans le résumé du job GitHub Actions et archivés en artefact, mais aucun seuil ne fait échouer le pipeline — cohérent avec le fait que ce test n'est pas exigé par les consignes, il sert d'indicateur de suivi dans le temps plutôt que de gate.
