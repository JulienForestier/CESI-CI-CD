# Cartographie des compétences et plan de formation

## Rôles nécessaires au projet

Le projet mobilise six rôles distincts. Certains peuvent être portés par la même personne à faible échelle (c'est le cas ici, en contexte de prototype), mais ils correspondent à des compétences réellement différentes — les séparer évite de supposer un profil "mouton à cinq pattes" dans un contexte d'équipe réelle.

| Rôle | Responsabilité sur ce projet | Compétences mobilisées |
|---|---|---|
| **Lead Developer / Architecte** | Choix d'architecture (Kustomize/ArgoCD, PostgreSQL vs alternative, JWT custom vs Identity), arbitrages techniques, revue de code | Vision transverse back/front/infra, C#/.NET, TypeScript, conception d'API REST, Git/GitFlow |
| **Développeur Backend** | API .NET 10 Minimal API, EF Core 10, migrations, endpoints auth/catalogue, tests xUnit | C#/.NET, EF Core, ASP.NET Core, JWT/`PasswordHasher`, xUnit, `WebApplicationFactory` |
| **Développeur Frontend** | UI React 19, TanStack Query, React Hook Form + Zod, Tailwind CSS v4 | React, TypeScript, gestion d'état serveur (React Query), validation de formulaires, Vitest/Testing Library |
| **Ingénieur DevOps / SRE** | Pipelines GitHub Actions, GitOps ArgoCD, Kustomize, cluster k3s, observabilité | YAML CI/CD, Kubernetes (Deployment, NetworkPolicy, Kustomize overlays), ArgoCD, Prometheus/Grafana, Loki |
| **Ingénieur Sécurité / DevSecOps** | Intégration SAST/SCA/scan secrets/scan conteneurs/scan IaC, Sealed Secrets, cert-manager, durcissement `securityContext`, signature d'image | Semgrep/CodeQL/Trivy/Kubescape, PKI/TLS, Pod Security Standards, supply chain (cosign/Sigstore) |
| **QA / Test Engineer** | Stratégie de test (types, outils, seuils de couverture), tests d'acceptation, tests de charge | Conception de plans de test, xUnit/Vitest, Siege/JMeter, lecture de rapports SonarCloud |

## Auto-évaluation des compétences actuelles

| Compétence | Niveau constaté sur ce projet | Justification |
|---|---|---|
| Backend .NET / EF Core | Acquis | API fonctionnelle, migrations générées, 96 % de couverture de tests |
| Frontend React / TypeScript | Acquis | UI complète (auth, catalogue, publication), 100 % de couverture statements |
| CI/CD GitHub Actions | Acquis | Pipeline multi-jobs conditionnel, quality gate bloquant opérationnel |
| Kubernetes / GitOps | Intermédiaire | Architecture fonctionnelle (ArgoCD, Kustomize, CNPG) mais découverte progressive en cours de projet (ex. migration du chart Loki déprécié, ajustements Pod Security Standards) |
| Sécurité applicative (DevSecOps) | Intermédiaire | Outils intégrés et opérationnels (Trivy, Semgrep, CodeQL, Kubescape, cosign) mais posture de sécurité encore partielle (voir [plan de remédiation](./08-plan-remediation-securite.md) — findings Medium/Low restants) |
| Tests de charge / performance | Débutant | Premier test de charge réalisé sur ce projet (Siege) ; pas encore de méthodologie de dimensionnement de charge ni de scénarios réalistes multi-endpoints avec authentification |

## Action de formation proposée

**Formation : "Kubernetes Security & Hardening" (3 jours, type CKS — Certified Kubernetes Security Specialist, ou équivalent formation courte type Wescale/OpenClassrooms)**

- **Public** : Ingénieur DevOps / SRE + Ingénieur Sécurité.
- **Justification** : le scan Kubescape a révélé, en cours de projet, une méconnaissance initiale de plusieurs contrôles standards (Pod Security Standards, `securityContext` complet, gestion des capacités Linux, signature d'image). Ces lacunes ont été comblées de façon réactive (après un scan en échec) plutôt que par une maîtrise préalable. Une formation ciblée permettrait d'appliquer ces bonnes pratiques *dès la conception* des manifests plutôt qu'en correction a posteriori, réduisant le risque de dette de sécurité sur les prochains services déployés.
- **Format réaliste** : formation courte (2-3 jours), pas de certification obligatoire — l'objectif est la maîtrise opérationnelle des contrôles (Pod Security Standards, admission control, supply chain/signature d'image), pas un titre.
- **Mesure de l'efficacité** : réduction du nombre de findings *High* Kubescape sur les prochains manifests déployés dès leur première version (cible : 0 finding High dès la première PR, contre 4 corrigés a posteriori sur ce projet).
