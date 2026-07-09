# Indicateurs qualité logicielle (ISO 25010)

Quatre indicateurs ont été retenus pour couvrir les quatre axes de qualité logicielle demandés : **fonctionnalité**, **performance**, **maintenabilité**, **fiabilité** (classification ISO/IEC 25010). Chacun est mesuré automatiquement par un outil du pipeline, pas déclaratif — c'est cette automatisation qui permet de détecter une dérive avant qu'elle ne devienne de la dette technique.

## 1. Couverture de tests sur le code nouveau — *Maintenabilité*

| | |
|---|---|
| **Outil** | SonarCloud (`sonar.cs.opencover.reportsPaths` pour l'API, `sonar.javascript.lcov.reportPaths` pour le front) |
| **Seuil** | ≥ 80 % de couverture sur le *new code* (condition du Quality Gate "Sonar way") |
| **Mesure constatée** | Quality Gate `OK` sur les PR : couverture du *new code* **≥ 80 %** (condition bloquante). Base de tests : **263 tests automatisés** (107 backend .NET, 156 front Vitest), ~98 % de couverture statements côté front, couverture API hors migrations EF Core générées |
| **Lien avec la dette technique** | Le calcul porte sur le *new code*, pas sur l'ensemble du dépôt : un développeur ne peut pas "diluer" une baisse de couverture dans une base de code ancienne déjà bien testée. Le Quality Gate est bloquant sur la PR (branch protection GitHub) : une régression de couverture ne peut pas être mergée, donc ne peut jamais s'accumuler silencieusement. |

## 2. Note de fiabilité, sécurité et maintenabilité (ratings SonarCloud) — *Fiabilité / Maintenabilité*

| | |
|---|---|
| **Outil** | SonarCloud — `new_reliability_rating`, `new_maintainability_rating`, `new_security_rating`, `new_duplicated_lines_density` |
| **Seuil** | Rating A (1) sur les trois notes ; duplication < 3 % sur le code nouveau |
| **Mesure constatée** | Sur le *new code* des PR : Reliability A, Security A, Maintainability A, duplication **< 3 %** (Quality Gate `OK`). L'analyse Sonar tourne aussi sur les push `dev`/`main` pour maintenir la note globale du projet à jour |
| **Lien avec la dette technique** | Ces notes reflètent la densité de *code smells*, bugs potentiels et duplication détectés statiquement. Les bloquer en Quality Gate empêche l'accumulation de "petits compromis" (copier-coller, complexité cyclomatique excessive, mauvaises pratiques du langage) qui, cumulés PR après PR, sont la définition même de la dette technique. |

## 3. Temps de réponse et taux d'échec sous charge — *Performance*

| | |
|---|---|
| **Outil** | Siege (test de charge manuel sur l'environnement `dev` déployé) |
| **Seuil indicatif** | Temps de réponse moyen < 1 s, taux d'échec = 0 % à 15 utilisateurs concurrents |
| **Mesure constatée** | 913 transactions, **0 échec**, temps de réponse moyen **0,49 s**, débit 29,81 trans/s (voir [Architecture technique](./05-architecture-technique.md) et section démonstration) |
| **Lien avec la dette technique** | Un temps de réponse qui se dégrade progressivement d'une version à l'autre est un signal de dette de performance (requêtes N+1, absence d'index, fuite mémoire). Rejouer ce test à chaque montée de version majeure permet de détecter cette dérive avant qu'elle n'affecte les utilisateurs, plutôt que de la découvrir en production. |

## 4. Disponibilité applicative (santé des probes Kubernetes) — *Fiabilité*

| | |
|---|---|
| **Outil** | `readinessProbe` / `livenessProbe` Kubernetes sur `/health` (API) et `/` (front), observés via `kubectl get pods` et le statut ArgoCD (`Healthy` / `Synced`) |
| **Seuil** | 0 redémarrage inattendu (`RESTARTS`), statut `Healthy` en continu |
| **Mesure constatée** | Les trois pods applicatifs (`myapp`, `apiservice`, `identityservice`) en `Running`, **0 restart** avant et après la campagne de charge et le durcissement de sécurité (`securityContext` non-root, `readOnlyRootFilesystem`) ; probes `/health` (API/IdP) et `/` (front) vertes |
| **Lien avec la dette technique** | Des redémarrages fréquents (CrashLoopBackOff, OOMKilled) trahissent une dette d'infrastructure (limites de ressources mal dimensionnées, fuite mémoire applicative) qui, non traitée, dégrade la disponibilité perçue par l'utilisateur au fil des déploiements. |

## Synthèse

| Indicateur | Axe ISO 25010 | Outil | Seuil | Bloquant en CI ? |
|---|---|---|---|---|
| Couverture new code | Maintenabilité | SonarCloud | ≥ 80 % | Oui (Quality Gate + branch protection) |
| Ratings Sonar (reliability/security/maintainability) | Fiabilité / Maintenabilité | SonarCloud | A partout | Oui (Quality Gate) |
| Temps de réponse sous charge | Performance | Siege | < 1 s, 0 % échec | Non (mesure manuelle en soutenance) |
| Disponibilité des pods | Fiabilité | Probes K8s + ArgoCD | 0 restart, Healthy | Non (supervision continue) |

Les deux premiers indicateurs sont **bloquants dans le pipeline CI** (voir [Processus de test](./02-processus-test.md)) : ils empêchent mécaniquement la fusion d'une régression. Les deux derniers sont mesurés lors des déploiements et de la démonstration ; ils justifient les choix d'architecture (probes, ressources, GitOps auto-heal) documentés dans le [schéma d'architecture](./05-architecture-technique.md).
