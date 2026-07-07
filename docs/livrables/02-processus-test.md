# Processus de test

## Types de tests, outils et parties prenantes

| Type de test | Objectif | Outil / Framework | Déclenchement | Partie prenante responsable |
|---|---|---|---|---|
| **Unitaire** (backend) | Valider la logique métier isolée (ex. règles de modération d'annonce) | xUnit + coverlet | Chaque push / PR (`ci-api.yml`) | Développeur backend |
| **Unitaire** (frontend) | Valider composants React, hooks, schémas Zod, client API | Vitest + Testing Library + `@vitest/coverage-v8` | Chaque push / PR (`ci-front.yml`) | Développeur frontend |
| **Intégration** (backend) | Valider un endpoint API de bout en bout (DB en mémoire, JWT réel, HTTP réel) | `Microsoft.AspNetCore.Mvc.Testing` (`WebApplicationFactory`) + EF Core InMemory | Chaque push / PR (`ci-api.yml`) | Développeur backend |
| **Acceptation** | Valider qu'une fonctionnalité respecte le backlog (critères d'acceptation) | Tests d'intégration ciblés sur les scénarios du backlog (ex. `PostListing_CreatesRejectedListing_WhenModerationFails`) — voir [Backlog](./06-backlog-fonctionnalite-metier.md) | Chaque push / PR | Développeur + Product Owner (revue manuelle en soutenance) |
| **Analyse statique de code (SAST)** | Détecter vulnérabilités et anti-patterns dans le code source | Semgrep OSS, CodeQL (JS/TS + C#), SonarCloud | Chaque push / PR (`ci-security.yml`, `code-quality-sonar.yml`) | Security champion / Lead Dev |
| **Analyse de composants (SCA)** | Détecter les dépendances vulnérables (npm, NuGet) | OWASP Dependency-Check, `npm audit` / `dotnet list package --vulnerable` | Chaque push / PR | Security champion |
| **Scan de secrets** | Détecter des identifiants/clés commis par erreur | TruffleHog | Chaque push / PR | Security champion |
| **Scan de conteneurs** | Détecter les CVE dans les images Docker construites | Trivy | Chaque push / PR (avant déploiement) | DevOps / Security champion |
| **Scan d'infrastructure (IaC)** | Détecter les mauvaises pratiques Kubernetes (privilèges, absence de `securityContext`, signature d'image, etc.) | Kubescape | Chaque push / PR sur le dépôt Kubernetes | DevOps |
| **Charge** | Mesurer temps de réponse et taux d'échec sous concurrence | Siege | Manuel, à la demande / en soutenance | DevOps / Lead Dev |

Le pipeline applique donc bien **plus des deux types de tests minimum exigés**, avec au moins deux catégories exécutées automatiquement et bloquantes (unitaire + intégration côté fonctionnel, SAST + SCA côté sécurité).

## Preuve d'exécution réussie (dernière exécution en date)

- **API (.NET)** : 29 tests (unitaires `ListingModerationServiceTests`, `TokenServiceTests` + intégration `AuthEndpointsTests`, `CatalogEndpointsTests`), **29/29 réussis**, couverture 95,9 % (hors migrations générées).
- **Front (React)** : 44 tests (composants, pages, hooks, contexte, schémas), **44/44 réussis**, couverture 100 % statements / 92,45 % branches.
- **SonarCloud Quality Gate** : `OK` sur la PR #7 (`new_coverage = 94.9`, seuil 80 ; toutes les autres conditions à `OK`).
- **Sécurité** (PR #7, 23 check runs) : Trivy, Semgrep, CodeQL (JS/TS + C#), TruffleHog, OWASP Dependency-Check — **tous passés**.
- **Charge** (Siege, `dev`) : 913 transactions, **0 échec**, 0,49 s de temps de réponse moyen.

## Politique de blocage

- Le **Quality Gate SonarCloud** (couverture ≥ 80 % sur le code nouveau + notes A) est une **règle de protection de branche GitHub** sur `dev` : une PR ne peut pas être fusionnée si le gate échoue, quel que soit l'avis du relecteur.
- Les scans de sécurité (Trivy, Semgrep, CodeQL, TruffleHog, Dependency-Check, Kubescape) sont exécutés à chaque PR ; un échec bloque également la fusion via les *required status checks*.
- Le test de charge n'est **pas intégré au pipeline CI/CD** (non exigé par les consignes) : il est rejoué manuellement avant une démonstration ou une montée de version majeure, sur l'environnement `dev` réellement déployé.
