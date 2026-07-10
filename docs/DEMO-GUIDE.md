# Guide de démo (oral)

Aide-mémoire pratique pour la soutenance — comptes disponibles, données de démo et commandes utiles. Ce document n'est **pas** un livrable noté (voir [docs/livrables/](livrables/README.md) pour ceux-ci), c'est un pense-bête opérationnel.

## Environnements

| Env | URL | Branche source (GitOps) |
|---|---|---|
| **dev** | https://dev.julienforestier.pro | `dev` |
| rec | https://rec.julienforestier.pro | `release/x.y.z` |
| prod | https://julienforestier.pro | `main` |

## Comptes disponibles sur `dev`

| Rôle | Email | Mot de passe | Notes |
|---|---|---|---|
| **Admin** | `admin@collector.shop` | `AdminDemo1234!` | Accès `/admin/moderation` (annonces en attente + signalements) |
| Vendeuse | `marie.vintage.demo1783625822@collector.shop` | `DemoSeed1234!` | 7 annonces (5 publiées, 2 en attente) |
| Vendeur | `karim.collector.demo1783625822@collector.shop` | `DemoSeed1234!` | 7 annonces (5 publiées, 2 en attente) |
| Vendeuse | `sophie.records.demo1783625822@collector.shop` | `DemoSeed1234!` | 6 annonces (5 publiées, 1 en attente) |

> Ces 3 comptes vendeurs peuvent aussi servir d'acheteurs (achat/signalement) sur les annonces des deux autres — ce sont des comptes normaux, pas de rôle spécial.
> Pour créer un compte à la volée pendant la démo : bouton **Connexion → Créer un compte** sur https://dev.julienforestier.pro.

## Données de démo sur `dev`

20 annonces créées (15 publiées + 5 en attente de modération), pour illustrer le scoring automatique :

| Titre | Score | Raison |
|---|---|---|
| Urgent : figurine à vendre vite | 60 | titre suspect |
| Paiement direct souhaité pour ce vinyle rare | 50 | titre suspect, prix élevé |
| Gratuit quasiment - collection vinyles à écouler | 50 | titre suspect, prix élevé |
| Arnaque ? Non, sneakers authentiques garanties | 45 | titre suspect, description succincte |
| Urgent vente rapide | 35 | titre suspect, description succincte, prix élevé |

Visible en se connectant en admin puis `/admin/moderation`. Les 15 autres annonces (figurines, vinyles/cassettes, sneakers) sont publiées et visibles dans le catalogue public.

> La feature de **signalement** (🚩) n'existe pas sur `dev` — elle vit uniquement sur les 3 branches de démo CI ci-dessous. Pour la montrer en live, soit en local (Aspire, voir [README](../README.md#lancer-en-local-aspire)), soit après avoir mergé une des branches.

## Démo des 3 branches (CI gates)

Trois branches identiques (même feature de signalement) avec des états différents, pour montrer que la pipeline bloque effectivement :

| Branche | Résultat attendu | Ce qui bloque |
|---|---|---|
| `feature/report-listing` | ✅ Verte | Rien — passe tous les gates |
| `feature/report-listing-low-coverage` | ❌ Bloquée | Couverture new code < 80 % (SonarCloud Quality Gate) |
| `feature/report-listing-sql-injection` | ❌ Bloquée | Faille SQL injection détectée (Semgrep/CodeQL) |

Le plus simple pour la démo (pas besoin d'être authentifié, repo public) — ouvrir directement dans le navigateur :

- Runs CI par branche : https://github.com/JulienForestier/CESI-CI-CD/actions?query=branch%3Afeature%2Freport-listing
- Idem pour `feature/report-listing-low-coverage` et `feature/report-listing-sql-injection` (changer le nom de branche dans l'URL).
- Quality Gate SonarCloud : https://sonarcloud.io/project/overview?id=JulienForestier_CESI-CI-CD

Alternative en CLI (nécessite `gh auth login` au préalable, pas fait sur cette machine) :
```bash
gh auth login   # une seule fois, si besoin

gh run list --branch feature/report-listing --limit 3
gh run list --branch feature/report-listing-low-coverage --limit 3
gh run list --branch feature/report-listing-sql-injection --limit 3

gh run view <run-id>   # détail d'un run (logs, jobs, conclusion)
```

## Commandes Kubernetes

```bash
# État des pods par environnement
kubectl get pods -n cesi-ci-cd-dev
kubectl get pods -n cesi-ci-cd-rec
kubectl get pods -n cesi-ci-cd-prod

# Logs live d'un service
kubectl logs -f deploy/apiservice -n cesi-ci-cd-dev
kubectl logs -f deploy/identityservice -n cesi-ci-cd-dev
kubectl logs -f deploy/myapp -n cesi-ci-cd-dev

# Statut de sync ArgoCD en CLI (sans ouvrir l'UI)
kubectl get application cesi-ci-cd-dev -n argocd -o jsonpath='{.status.sync.status} {.status.health.status}{"\n"}'
kubectl get application cesi-ci-cd-rec -n argocd -o jsonpath='{.status.sync.status} {.status.health.status}{"\n"}'
kubectl get application cesi-ci-cd-prod -n argocd -o jsonpath='{.status.sync.status} {.status.health.status}{"\n"}'

# Lister toutes les Applications ArgoCD d'un coup
kubectl get applications -n argocd
```

## Accès aux interfaces (port-forward)

```bash
# ArgoCD — https://localhost:8080 (user: admin)
kubectl port-forward svc/argocd-server -n argocd 8080:443
kubectl -n argocd get secret argocd-initial-admin-secret -o jsonpath="{.data.password}" | base64 -d

# Grafana — http://localhost:3000 (user: admin)
kubectl port-forward svc/kube-prometheus-stack-grafana -n monitoring 3000:80
kubectl get secret kube-prometheus-stack-grafana -n monitoring -o jsonpath="{.data.admin-password}" | base64 -d

# Prometheus — http://localhost:9090
kubectl port-forward svc/kube-prometheus-stack-prometheus -n monitoring 9090:9090
```

## Requêtes utiles dans Grafana (Explore → datasource Loki)

```logql
# Tous les logs de l'environnement dev
{namespace="cesi-ci-cd-dev"}

# Logs d'un pod précis
{namespace="cesi-ci-cd-dev", pod=~"apiservice.*"}

# Uniquement les erreurs
{namespace="cesi-ci-cd-dev"} |= "error"

# Débit de logs par pod sur les 5 dernières minutes
sum by (pod) (rate({namespace="cesi-ci-cd-dev"}[5m]))
```

## Repli en local (si `dev` a un souci pendant la démo)

```bash
dotnet run --project api/CESI-CI-CD
```
Voir [README.md](../README.md#lancer-en-local-aspire) pour le détail (secrets requis, build de l'UI de login).
