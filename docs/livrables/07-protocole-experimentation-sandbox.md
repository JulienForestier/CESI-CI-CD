# Protocole d'expérimentation en bac à sable

## Environnement de test

Toutes les expérimentations ci-dessous ont été menées **directement sur le cluster cible** (k3s mono-nœud, VPS OVH, accès `kubectl` via un kubeconfig dédié `~/.kube/config-ovh`), plutôt que sur un cluster jetable de type Minikube/Kind. Ce choix a été fait car le contexte (VPS unique, ressources limitées) rendait la reproduction sur un second cluster peu représentative des contraintes réelles (mémoire disponible, latence réseau OVH, DNS public pour les certificats Let's Encrypt).

## Expérimentation 1 — CloudNativePG (clustering PostgreSQL sur Kubernetes)

- **Objectif** : valider qu'un opérateur Kubernetes peut gérer un cluster PostgreSQL (haute disponibilité applicative) plutôt qu'un simple `StatefulSet` fait main ou un chart Helm générique.
- **Alternatives évaluées** : (a) chart Helm communautaire générique, (b) `StatefulSet` écrit à la main, (c) opérateur **CloudNativePG (CNPG)**.
- **Étapes clés** :
  1. Installation de l'opérateur CNPG (namespace `cnpg-system`).
  2. Déclaration d'une ressource custom `Cluster` (1 instance en `dev`/`rec`, 2 instances en `prod` via un patch Kustomize dédié).
  3. Vérification de la génération automatique du Secret applicatif `<cluster>-app` (clés `host`, `port`, `dbname`, `username`, `password`, `uri`…).
  4. Composition de la chaîne de connexion côté API via interpolation `$(VAR)` Kubernetes à partir de ces clés.
- **Difficultés rencontrées** : aucune difficulté bloquante ; la principale prise en main a porté sur la composition de la chaîne de connexion à partir de plusieurs clés de Secret séparées (pas une URI unique directement exploitable par Npgsql/EF Core sans recomposition).
- **Limites identifiées** : le clustering à 2 instances en `prod` reste démonstratif — un seul nœud Kubernetes physique héberge les deux instances (pas de haute disponibilité infrastructure réelle, uniquement applicative au niveau du pod).
- **Résultat / décision** : **adopté**. Un opérateur dédié gère nativement le failover, les sauvegardes et la génération de secrets, ce qu'un `StatefulSet` manuel aurait demandé de réimplémenter — gain de fiabilité net pour un coût de mise en œuvre faible.

## Expérimentation 2 — cert-manager + Let's Encrypt (TLS automatique sur Ingress)

- **Objectif** : sécuriser l'accès HTTPS aux trois environnements sans gestion manuelle de certificats.
- **Étapes clés** :
  1. Installation de cert-manager (CRDs + contrôleur).
  2. Déclaration d'un `ClusterIssuer` Let's Encrypt **staging** (pour valider le flux sans consommer le quota de production, limité).
  3. Validation du flux complet (challenge HTTP-01 via l'Ingress), puis bascule vers un `ClusterIssuer` **prod**.
  4. Vérification via `curl -I` que le certificat servi est bien signé par Let's Encrypt (pas l'autorité `staging`, non reconnue des navigateurs).
- **Difficultés rencontrées** : nécessité de bien distinguer les deux `ClusterIssuer` (staging/prod) pour éviter d'épuiser le quota de génération de certificats de production pendant les tests.
- **Limites identifiées** : dépendance à la résolution DNS publique du domaine vers l'IP du VPS — un changement d'IP nécessite une intervention manuelle sur la zone DNS avant que le renouvellement automatique ne fonctionne à nouveau.
- **Résultat / décision** : **adopté**. Le renouvellement est depuis entièrement automatique, sans action manuelle récurrente.

## Expérimentation 3 — Observabilité (kube-prometheus-stack + Loki/Promtail)

- **Objectif** : disposer de métriques et de logs centralisés pour les trois environnements.
- **Étapes clés** :
  1. Installation de `kube-prometheus-stack` (Prometheus + Grafana + Alertmanager).
  2. Installation initiale du chart `loki-stack` pour les logs.
  3. Migration vers le chart moderne `grafana-community/loki` (mode *Monolithic*, stockage filesystem) après détection de la dépréciation du premier chart.
- **Difficultés rencontrées** (documentées telles que réellement constatées, pas reformulées a posteriori) :
  - Conflit de datasource par défaut : `loki-stack` déclarait sa datasource Grafana comme `isDefault: true`, entrant en conflit avec celle de Prometheus (*"Only one datasource per organization can be marked as default"*) — corrigé en désactivant l'option côté Loki.
  - Découverte que `loki-stack` est **déprécié** et embarque une version de Loki trop ancienne, cassant le health-check de la datasource Grafana (`parse error at line 1, col 1: syntax error: unexpected IDENTIFIER`) — a motivé la migration vers le chart moderne.
  - Le nouveau chart `grafana-community/loki` exige des valeurs `loki.storage.bucketNames` renseignées même en stockage filesystem (bug connu du chart) — contourné avec des valeurs de convenance documentées dans `bootstrap/loki-values.yaml`.
  - Le composant `chunks-cache` a provoqué un défaut de planification par manque de mémoire disponible sur le nœud unique — désactivé (`chunksCache.enabled=false`, `resultsCache.enabled=false`), acceptable vu le volume de logs très faible de ce contexte.
- **Limites identifiées** : le stockage filesystem (pas d'object storage S3-compatible) limite la rétention et la scalabilité horizontale de Loki — non bloquant pour un cluster mono-nœud à faible volumétrie, à revoir si le volume de logs augmentait significativement.
- **Résultat / décision** : **adopté** (version moderne du chart), avec les ajustements ci-dessus documentés pour reproductibilité future.

## Expérimentation 4 — Sécurité supply chain : Kubescape (scan IaC) + cosign (signature d'image)

- **Objectif** : valider qu'un scanner de manifests Kubernetes et une signature d'image peuvent être intégrés au pipeline sans clé privée à gérer manuellement (signature *keyless*).
- **Étapes clés** :
  1. Intégration de Kubescape en pipeline GitHub Actions (scan des manifests à chaque push/PR, seuil `severity-threshold High`).
  2. Analyse des *findings* remontés (`kubescape scan . --format json`) pour distinguer les échecs bloquants (`High`) des simples signaux (`Medium`/`Low`).
  3. Correction des 4 findings `High` identifiés (admission de conteneurs privilégiés, absence de `securityContext`, absence de signature d'image — détail dans le [plan de remédiation](./08-plan-remediation-securite.md)).
  4. Intégration de `sigstore/cosign-installer` + signature keyless (OIDC GitHub Actions, sans clé privée stockée) juste après le push de chaque image vers GHCR.
  5. Validation en conditions réelles : dry-run serveur (`kubectl apply --dry-run=server`) puis déploiement réel, vérification des pods (`0` redémarrage) et test fonctionnel de bout en bout (`/api/auth/register` répond correctement sous la nouvelle politique de sécurité).
- **Difficultés rencontrées** :
  - Le workflow Kubescape échouait silencieusement à publier ses résultats dans l'onglet Sécurité GitHub (`Resource not accessible by integration`) faute de permission `security-events: write` explicite sur le workflow.
  - Distinction nécessaire entre les contrôles listés dans la "posture overview" (tous les contrôles applicables) et les contrôles réellement en échec (`status: failed`) — la lecture initiale du rapport humain laissait croire à ~30 échecs, alors que seuls 4 étaient réellement bloquants au seuil `High`.
- **Limites identifiées** : la validation du finding "signature d'image" nécessite qu'un déploiement complet ait eu lieu après l'ajout de la signature (les anciennes images, non signées, restent référencées tant qu'un nouveau déploiement n'a pas eu lieu) — délai de propagation normal d'une chaîne GitOps, pas un défaut de l'outil.
- **Résultat / décision** : **adopté**. Validé en conditions réelles : passage de 4 à 0 finding `High` après déploiement complet de la chaîne (durcissement des manifests + signature des images).

## Synthèse

| Technologie | Alternative écartée | Décision | Preuve de validation |
|---|---|---|---|
| CloudNativePG | Chart Helm générique / StatefulSet manuel | Adopté | Cluster PostgreSQL fonctionnel, 2 instances en prod |
| cert-manager + Let's Encrypt | Certificats manuels | Adopté | TLS automatique et renouvelé sans intervention |
| grafana-community/loki | loki-stack (déprécié) | Adopté (après migration) | Datasource Grafana fonctionnelle, logs consultables |
| Kubescape + cosign | Signature par clé privée statique | Adopté (keyless) | 0 finding `High` après déploiement, pods sains en conditions réelles |
