# Plan de remédiation sécurité

## 1. Synthèse des résultats de sécurité actuels

| Contrôle | Outil | Résultat (dernière exécution) |
|---|---|---|
| Analyse statique du code (SAST) | Semgrep OSS, CodeQL (JS/TS + C#) | ✅ Aucun finding bloquant |
| Analyse des workflows CI (SAST) | SonarCloud (règles `githubactions`) | ✅ Actions épinglées au SHA, permissions `write` au niveau job (après durcissement) |
| Dépendances vulnérables (SCA) | OWASP Dependency-Check, `npm audit`, `dotnet list package --vulnerable` | ✅ Aucune CVE critique |
| Secrets committés | TruffleHog | ✅ Aucun secret détecté |
| Vulnérabilités des images Docker | Trivy | ✅ Aucune CVE critique/haute non corrigée |
| Qualité et couverture de code | SonarCloud Quality Gate | ✅ `OK` (couverture new code ≥ 80 %, ratings A) |
| Posture de sécurité de l'infrastructure (IaC) | Kubescape | ⚠️ 0 finding `High`/`Critical`, **findings `Medium`/`Low`** résiduels (détail §2) |
| Provenance des images | cosign (signature keyless) | ✅ Images signées depuis le dernier déploiement |

Le pipeline bloque déjà la fusion de code en cas de régression sur les lignes de code applicatif comme sur les workflows (*required status checks* + Quality Gate SonarCloud). Les axes encore ouverts sont la posture IaC résiduelle (§2), volontairement non bloquante pour l'instant, et un point d'attention historique issu de l'analyse du test de charge (§3), désormais corrigé.

## 2. Findings résiduels (scan Kubescape, analysés individuellement)

Un premier passage avait révélé 4 findings `High`, tous corrigés et validés en conditions réelles (déploiement réel, pods sains, test fonctionnel réussi — voir [protocole d'expérimentation](./07-protocole-experimentation-sandbox.md)). L'analyse des findings `Medium`/`Low` restants distingue les vrais risques des faux positifs :

| ID | Contrôle | Sévérité | Analyse |
|---|---|---|---|
| C-0198, C-0197, C-0200, C-0201 | Admission de conteneurs root / `allowPrivilegeEscalation` / capacités | Medium | Nos conteneurs respectent déjà ces règles au niveau du pod (`runAsNonRoot`, `allowPrivilegeEscalation: false`, `capabilities.drop: [ALL]`) — le finding provient du **label de namespace** `pod-security.kubernetes.io/enforce=baseline`, qui n'active pas l'admission stricte au niveau cluster (seul `restricted` le ferait). **Risque réel mais faible** : un pod mal configuré à l'avenir ne serait pas bloqué à l'admission, seulement détecté après coup. |
| C-0206 | Absence de `NetworkPolicy` par défaut sur toutes les namespaces | Medium | Des `NetworkPolicy` existent déjà pour restreindre l'**entrée** vers les services (dont `apiservice`→`identityservice`), mais il n'existe pas de politique par défaut *deny-all* couvrant tout le trafic (y compris la **sortie**/egress). **Risque réel** : un pod compromis pourrait initier des connexions sortantes non filtrées. |
| C-0207 | Secrets en variables d'environnement plutôt qu'en fichiers montés | Medium | Les identifiants DB, le secret client OAuth du BFF et le certificat de signature de l'IdP sont injectés en variables d'environnement (lisibles via `/proc/<pid>/environ` en cas de compromission du conteneur). **Risque réel mais modéré** : nécessite déjà un accès au conteneur pour être exploité. |
| C-0189 | Compte de service `default` utilisé (au lieu d'un ServiceAccount dédié) | Medium | `automountServiceAccountToken: false` a déjà été appliqué (le token n'est plus monté), mais le pod utilise toujours le ServiceAccount `default` du namespace. **Risque résiduel faible** : sans token monté, l'identité du ServiceAccount n'est pas exploitable depuis le pod. |
| C-0054, C-0049 | "Cluster internal networking" / "Network mapping" | Medium / Low | Contrôles informatifs de cartographie réseau (pas une vulnérabilité en soi) — servent de support à l'analyse de C-0206. |
| C-0061 | "Pods in default namespace" | Low | **Faux positif identifié** : Kubescape scanne les manifests `base/` de Kustomize sans namespace déclaré (normal, le namespace réel est injecté par chaque overlay). Vérifié : les pods réels tournent bien dans `cesi-ci-cd-{dev|rec|prod}`. |
| C-0077 | Absence des labels standards `app.kubernetes.io/*` | Low | Cosmétique — n'affecte pas la sécurité, facilite l'outillage. |

## 3. Vulnérabilité identifiée via l'analyse du test de charge — et corrigée

Le test de charge (Siege, 15 utilisateurs concurrents, voir [indicateurs qualité](./01-indicateurs-qualite.md)) n'a ciblé que des **endpoints publics en lecture** (`/`, `/api/categories`, `/api/listings`). Il ne disait rien de la résistance des endpoints d'écriture sensibles — notamment l'**authentification** :

- Constat initial : les endpoints d'inscription/connexion **ne comportaient aucune limitation de débit** (bourrage d'identifiants, création massive de comptes possibles sans ralentissement applicatif).
- **Remédiation appliquée** : avec la migration vers OpenID Connect, l'authentification est désormais portée par l'IdentityServer (`/account/register`, `/account/login`) et **protégée par un rate limiter** (`Microsoft.AspNetCore.RateLimiting`, fenêtre fixe, groupe `auth`). Un bourrage d'identifiants est donc désormais ralenti côté applicatif, indépendamment des limites de ressources Kubernetes.
- Cette action illustre la démarche attendue : **ce que le test ne couvre pas est aussi informatif que ce qu'il couvre** — l'analyse a identifié un risque, qui a ensuite été corrigé.

## 4. Bonnes pratiques de sécurité intégrées au POC

*(2 minimum exigées ; liste réelle largement supérieure)*

1. **HTTPS/TLS** sur les trois environnements (cert-manager + Let's Encrypt).
2. **Authentification OAuth 2.0 / OpenID Connect** (Duende IdentityServer, Authorization Code + PKCE) via le **pattern BFF** : tokens gardés côté serveur, cookie de session `HttpOnly`/`SameSite=Strict`/`Secure`, **aucun token exposé au navigateur** (élimine le vol de token par XSS de l'ancienne approche JWT-en-`localStorage`). Protection CSRF par header dédié.
3. **Rate limiting** sur les endpoints d'inscription/connexion (anti bourrage d'identifiants).
4. **Secrets chiffrés en Git** (Bitnami Sealed Secrets) — aucun secret en clair dans le contrôle de version.
5. **Conteneurs non-root, système de fichiers en lecture seule, capacités Linux nulles** (`securityContext` complet), token de ServiceAccount non monté.
6. **Durcissement de la chaîne CI/CD** : actions GitHub épinglées au SHA de commit (anti re-tag malveillant), permissions `write` scopées au niveau job (moindre privilège), `npm ci --ignore-scripts`.
7. **Scan de vulnérabilités** à plusieurs niveaux : code (SAST), dépendances (SCA), secrets, images (Trivy) — bloquants en CI.
8. **Signature d'image** (cosign, keyless) garantissant la provenance avant déploiement.
9. **Scan IaC continu** (Kubescape) sur les manifests Kubernetes.

## 5. Plan de remédiation priorisé

| Priorité | Action | Justification | État / Effort |
|---|---|---|---|
| **Fait** | Rate limiting applicatif sur l'inscription/connexion | Seule vulnérabilité **activement exploitable** identifiée (§3). | ✅ Intégré côté IdentityServer |
| **P1 — Court terme** | Ajouter une `NetworkPolicy` par défaut `deny-all` (ingress **et** egress) par namespace, puis des règles explicites d'autorisation | Corrige C-0206/C-0054/C-0049 ; limite la propagation latérale en cas de compromission d'un pod. | Faible (manifests Kustomize additionnels) |
| **P2 — Moyen terme** | Créer un `ServiceAccount` dédié et nommé par Deployment (au lieu du `default`) | Corrige C-0189 ; moindre privilège, même si le risque immédiat est déjà atténué par `automountServiceAccountToken: false`. | Faible |
| **P3 — Moyen terme** | Migrer les secrets sensibles (secret client BFF, certificat de signature IdP, identifiants DB) de variables d'environnement vers des volumes montés | Corrige C-0207 ; réduit la surface d'exposition en cas d'accès au conteneur. | Moyen (adapter la lecture de configuration côté .NET, `IConfiguration` supporte les *file providers*) |
| **P4 — Moyen/long terme** | Valider la compatibilité de CloudNativePG avec le Pod Security Standard `restricted`, puis migrer les namespaces de `baseline` à `restricted` | Corrige C-0197/198/200/201 définitivement (admission stricte). | Moyen (test de non-régression requis) |
| **P5 — Long terme** | Ajouter les labels Kubernetes standards (`app.kubernetes.io/*`) | Corrige C-0077 ; améliore l'outillage, pas un enjeu de sécurité direct. | Faible |
| **P6 — Avant prod réelle** | Statuer sur la **licence Duende** (IdentityServer/BFF en mode évaluation) | Hors périmètre pédagogique, mais un usage production réel impose une licence commerciale. | Décision (contractuel) |

Les priorités sont établies par **analyse humaine** plutôt que par la seule sortie brute des scanners : le rate limiting (risque réellement exploitable) a été traité en premier, puis le cloisonnement réseau (mouvement latéral) — conformément à la démarche attendue : le scanner signale, l'analyse priorise.
