# Plan de remédiation sécurité

## 1. Synthèse des résultats de sécurité actuels

| Contrôle | Outil | Résultat (dernière exécution) |
|---|---|---|
| Analyse statique du code (SAST) | Semgrep OSS, CodeQL (JS/TS + C#) | ✅ Aucun finding bloquant |
| Dépendances vulnérables (SCA) | OWASP Dependency-Check, `npm audit`, `dotnet list package --vulnerable` | ✅ Aucune CVE critique |
| Secrets committés | TruffleHog | ✅ Aucun secret détecté |
| Vulnérabilités des images Docker | Trivy | ✅ Aucune CVE critique/haute non corrigée |
| Qualité et couverture de code | SonarCloud Quality Gate | ✅ `OK` (couverture new code 94,9 %, ratings A) |
| Posture de sécurité de l'infrastructure (IaC) | Kubescape | ⚠️ 0 finding `High`/`Critical`, **8 `Medium` et 3 `Low`** résiduels (détail §2) |
| Provenance des images | cosign (signature keyless) | ✅ Images signées depuis le dernier déploiement |

Le pipeline bloque déjà la fusion de code en cas de régression sur les quatre premières lignes (*required status checks*). Le seul axe encore ouvert est la posture IaC résiduelle, volontairement non bloquante pour l'instant (voir §2), et un point d'attention identifié via l'analyse du test de charge (§3).

## 2. Findings résiduels (scan Kubescape, analysés individuellement)

Un premier passage avait révélé 4 findings `High`, tous corrigés et validés en conditions réelles (déploiement réel, pods sains, test fonctionnel réussi — voir [protocole d'expérimentation](./07-protocole-experimentation-sandbox.md)). L'analyse détaillée des 8 findings `Medium` et 3 `Low` restants distingue les vrais risques des faux positifs :

| ID | Contrôle | Sévérité | Analyse |
|---|---|---|---|
| C-0198, C-0197, C-0200, C-0201 | Admission de conteneurs root / `allowPrivilegeEscalation` / capacités | Medium | Nos conteneurs respectent déjà ces règles au niveau du pod (`runAsNonRoot`, `allowPrivilegeEscalation: false`, `capabilities.drop: [ALL]`) — le finding provient du **label de namespace** `pod-security.kubernetes.io/enforce=baseline`, qui n'active pas l'admission stricte de ces règles au niveau cluster (seul `restricted` le ferait). **Risque réel mais faible** : un pod mal configuré à l'avenir ne serait pas bloqué à l'admission, seulement détecté après coup par un scan. |
| C-0206 | Absence de `NetworkPolicy` par défaut sur toutes les namespaces | Medium | Des `NetworkPolicy` existent déjà pour restreindre l'**entrée** vers `apiservice`/`myapp`, mais il n'existe pas de politique par défaut *deny-all* couvrant l'ensemble du trafic (y compris la **sortie**/egress). **Risque réel** : un pod compromis pourrait initier des connexions sortantes non filtrées. |
| C-0207 | Secrets en variables d'environnement plutôt qu'en fichiers montés | Medium | Les identifiants DB et la clé JWT sont injectés en variables d'environnement (lisibles via `/proc/<pid>/environ` en cas de compromission du conteneur), plutôt qu'en fichiers montés (accès plus contrôlé). **Risque réel mais modéré** : nécessite déjà un accès au conteneur pour être exploité. |
| C-0189 | Compte de service `default` utilisé (au lieu d'un ServiceAccount dédié) | Medium | `automountServiceAccountToken: false` a déjà été appliqué (le token n'est plus monté), mais le pod utilise toujours le ServiceAccount `default` du namespace plutôt qu'un ServiceAccount nommé et dédié. **Risque résiduel faible** : sans token monté, l'identité du ServiceAccount n'est de toute façon pas exploitable depuis le pod. |
| C-0054, C-0049 | "Cluster internal networking" / "Network mapping" | Medium / Low | Contrôles informatifs de cartographie réseau (pas une vulnérabilité en soi) — servent de support à l'analyse de C-0206. |
| C-0061 | "Pods in default namespace" | Low | **Faux positif identifié** : Kubescape scanne les manifests `base/` de Kustomize de façon isolée, sans namespace déclaré — ce qui est normal, le namespace réel est injecté par chaque overlay (`dev`/`rec`/`prod`) au moment du rendu. Les pods réels ne sont jamais déployés dans `default`, ce qui a été vérifié (`kubectl get pods -n cesi-ci-cd-dev`). |
| C-0077 | Absence des labels standards `app.kubernetes.io/*` | Low | Cosmétique — n'affecte pas la sécurité, facilite l'outillage (sélection, observabilité). |

## 3. Vulnérabilité potentielle identifiée via l'analyse du test de charge

Le test de charge réalisé (Siege, 15 utilisateurs concurrents, voir [indicateurs qualité](./01-indicateurs-qualite.md)) n'a ciblé que des **endpoints publics en lecture** (`/`, `/api/categories`, `/api/listings`). Il ne dit donc rien de la résistance des endpoints d'écriture les plus sensibles :

- `POST /api/auth/login` et `POST /api/auth/register` **ne sont soumis à aucune limitation de débit** (pas de *rate limiting*, pas de verrouillage progressif après échecs répétés).
- **Vulnérabilité potentielle** : un attaquant pourrait mener un bourrage d'identifiants (*credential stuffing*) sur `/login`, ou générer un grand nombre de comptes/annonces via `/register` et `/listings` (spam, épuisement de ressources), sans qu'aucun mécanisme applicatif ne le ralentisse. Seules les limites de ressources Kubernetes (CPU/mémoire) constitueraient un frein indirect, insuffisant contre une attaque distribuée à faible débit.
- Cette analyse découle directement du périmètre du test de charge réalisé : **ce que le test ne couvre pas est aussi informatif que ce qu'il couvre**.

## 4. Bonnes pratiques de sécurité déjà intégrées au POC

*(2 minimum exigées ; liste réelle largement supérieure)*

1. **HTTPS/TLS** sur les trois environnements (cert-manager + Let's Encrypt).
2. **Authentification par jeton JWT** signé (HS256), mots de passe hashés (`PasswordHasher<T>`), jamais stockés en clair.
3. **Secrets chiffrés en Git** (Bitnami Sealed Secrets) — aucun secret en clair dans le contrôle de version.
4. **Conteneurs non-root, système de fichiers en lecture seule, capacités Linux nulles** (`securityContext` complet).
5. **Scan de vulnérabilités** à quatre niveaux : code (SAST), dépendances (SCA), secrets, images (Trivy) — bloquants en CI.
6. **Signature d'image** (cosign, keyless) garantissant la provenance avant déploiement.
7. **Scan IaC continu** (Kubescape) sur les manifests Kubernetes.

## 5. Plan de remédiation priorisé

| Priorité | Action | Justification | Effort estimé |
|---|---|---|---|
| **P1 — Court terme** | Ajouter un *rate limiting* applicatif sur `/api/auth/login`, `/api/auth/register` et `POST /api/listings` (ex. `AspNetCoreRateLimit` ou middleware natif .NET 8+ `Microsoft.AspNetCore.RateLimiting`) | Seule vulnérabilité **activement exploitable** identifiée (§3) ; les autres findings sont des mesures de défense en profondeur, celle-ci est une porte ouverte concrète. | Faible (middleware standard, pas de nouvelle infrastructure) |
| **P2 — Court terme** | Ajouter une `NetworkPolicy` par défaut `deny-all` (ingress **et** egress) par namespace, puis des règles explicites d'autorisation | Corrige C-0206/C-0054/C-0049 ; limite la propagation latérale en cas de compromission d'un pod. | Faible (manifests Kustomize additionnels) |
| **P3 — Moyen terme** | Créer un `ServiceAccount` dédié et nommé par Deployment (au lieu du `default` du namespace) | Corrige C-0189 ; bonne pratique de moindre privilège, même si le risque immédiat est déjà atténué par `automountServiceAccountToken: false`. | Faible |
| **P4 — Moyen terme** | Migrer les secrets sensibles (clé JWT, identifiants DB) de variables d'environnement vers des volumes montés (`secretKeyRef` → `volumeMounts`) | Corrige C-0207 ; réduit la surface d'exposition en cas d'accès au conteneur. | Moyen (nécessite d'adapter la lecture de configuration côté API, `IConfiguration` supporte déjà les *file providers*) |
| **P5 — Moyen/long terme** | Valider la compatibilité de l'opérateur CloudNativePG avec le niveau Pod Security Standard `restricted`, puis migrer les namespaces de `baseline` à `restricted` | Corrige C-0197/198/200/201 définitivement (admission stricte, pas seulement détection a posteriori) ; nécessite une validation préalable pour ne pas casser le déploiement PostgreSQL. | Moyen (test de non-régression requis avant bascule) |
| **P6 — Long terme** | Ajouter les labels Kubernetes standards (`app.kubernetes.io/name`, `version`, `part-of`) sur toutes les ressources | Corrige C-0077 ; améliore l'outillage (sélection, observabilité), pas un enjeu de sécurité direct. | Faible |

Les priorités P1 et P2 sont recommandées en premier car elles correspondent à des **risques réellement exploitables** (bourrage d'identifiants, mouvement latéral réseau), identifiés par l'analyse plutôt que par la seule sortie brute des scanners — conformément à la démarche attendue : le scanner signale, l'analyse humaine priorise.
