# Livrables pédagogiques — Superviser et assurer le développement des applications logicielles

Documents produits pour l'évaluation de bloc, organisés par thème conformément aux critères d'évaluation. Chaque document s'appuie sur des résultats réellement mesurés sur ce projet (pipeline CI/CD, scans de sécurité, test de charge), pas sur des exemples génériques.

| # | Document | Compétence évaluée couverte |
|---|---|---|
| [01](./01-indicateurs-qualite.md) | Indicateurs qualité (ISO 25010) | Élaborer le processus d'assurance qualité logicielle |
| [02](./02-processus-test.md) | Processus de test (types, outils, parties prenantes) | Élaborer le processus d'assurance qualité logicielle |
| [03](./03-cycle-vie-devsecops-pipeline.md) | Cycle de vie DevSecOps + schéma pipeline CI/CD | Piloter le développement et le déploiement d'applications |
| [04](./04-competences-formation.md) | Cartographie des compétences + plan de formation | Piloter le développement et le déploiement d'applications |
| [05](./05-architecture-technique.md) | Architecture technique (découpage, sécurité, hébergement, orchestration) | Maintenir et développer son expertise |
| [06](./06-backlog-fonctionnalite-metier.md) | Backlog (user stories + critères d'acceptation) | Maintenir et développer son expertise |
| [07](./07-protocole-experimentation-sandbox.md) | Protocole d'expérimentation en bac à sable | Maintenir et développer son expertise |
| [08](./08-plan-remediation-securite.md) | Plan de remédiation sécurité | Élaborer le processus d'assurance qualité logicielle |

## Preuves à l'appui (résultats réels, session de développement)

- **Pull Request #7** (fonctionnalité métier) : SonarCloud Quality Gate `OK`, couverture new code **94,9 %**, 23/23 checks CI verts.
- **Test de charge** (Siege, `https://dev.julienforestier.pro`) : 913 transactions, **0 échec**, 0,49 s de temps de réponse moyen.
- **Scan Kubescape** : 4 findings `High` détectés puis corrigés et validés en conditions réelles (0 redémarrage de pod, endpoint `/api/auth/register` fonctionnel après durcissement).
- **Déploiement GitOps** : ArgoCD `Synced`/`Healthy` sur l'environnement `dev` après chaque fusion, self-heal vérifié en conditions réelles.
