# Backlog — fonctionnalité métier implémentée

## Contexte

Collector.shop est une marketplace C2C d'objets de collection. Le périmètre de ce prototype couvre la **tranche verticale minimale** permettant de démontrer le cœur du modèle économique : inscription/connexion, consultation publique du catalogue, et publication d'une annonce avec contrôle qualité automatique (exigé par le contexte métier pour limiter l'intervention humaine avant mise en ligne).

## User stories et critères d'acceptation

### US1 — Consulter le catalogue public

> En tant que **visiteur**, je veux consulter les annonces publiées sans créer de compte, afin de découvrir les objets disponibles avant de m'engager.

- **CA1** : le catalogue n'affiche que les annonces au statut `Published`.
- **CA2** : je peux filtrer les résultats par catégorie.
- **CA3** : si aucune annonce ne correspond au filtre, un message explicite s'affiche (pas de page vide silencieuse).

**Preuve de conformité (tests)** : `GetListings_ReturnsOnlyPublishedListings` (API), `CatalogPage` — *"shows an empty state when there are no listings"*, *"refetches listings when the category filter changes"* (front).

### US2 — Créer un compte

> En tant que **visiteur**, je veux créer un compte avec email et mot de passe, afin d'accéder aux fonctionnalités réservées aux membres (publier une annonce).

- **CA1** : l'email doit être unique (conflit `409` sinon).
- **CA2** : le mot de passe doit contenir au moins 8 caractères.
- **CA3** : en cas de succès, une **session est ouverte** (cookie `HttpOnly` posé par le flow OIDC) et je suis redirigé vers le catalogue.

**Preuve de conformité** : `Register_CreatesUser_AndReturnsRoot_WhenNoReturnUrl`, `Register_ReturnsConflict_WhenEmailAlreadyExists`, `Register_ReturnsBadRequest_WhenFieldMissing`, `Register_ReturnsBadRequest_WhenPasswordTooShort` (IdentityService.Tests) ; formulaire d'inscription testé côté `identity-ui` (`RegisterForm.test.tsx`).

### US3 — Se connecter

> En tant qu'**utilisateur inscrit**, je veux me connecter avec mes identifiants, afin de retrouver mon espace.

- **CA1** : identifiants valides → **session ouverte** (cookie de session émis par l'IdentityServer via le BFF).
- **CA2** : email inconnu **ou** mot de passe incorrect → `401` avec un message générique identique dans les deux cas (pas d'énumération des comptes existants).

**Preuve de conformité** : `Login_ReturnsOk_WithValidCredentials`, `Login_ReturnsUnauthorized_WhenEmailUnknown`, `Login_ReturnsUnauthorized_WhenPasswordWrong` (IdentityService.Tests) ; formulaire de connexion testé côté `identity-ui` (`LoginForm.test.tsx`).

### US4 — Publier une annonce (avec contrôle qualité automatique)

> En tant qu'**utilisateur connecté**, je veux publier une annonce (titre, description, prix, catégorie), afin de proposer un objet à la vente, en sachant qu'elle est soumise à un contrôle qualité automatique avant diffusion.

- **CA1** : je dois être connecté pour publier (`401` sinon).
- **CA2** : un contrôle qualité automatique évalue l'annonce (titre ≥ 3 caractères, description non vide, prix compris entre 0,01 € et 100 000 €).
- **CA3** : si l'annonce passe le contrôle, elle apparaît **immédiatement** dans le catalogue public, sans intervention humaine.
- **CA4** : si l'annonce échoue le contrôle, elle n'apparaît **pas** dans le catalogue et un message m'indique explicitement de vérifier le titre, la description et le prix.
- **CA5** : la catégorie choisie doit exister (`400` sinon).

**Preuve de conformité** : `PostListing_CreatesPublishedListing_WhenValid`, `PostListing_CreatesRejectedListing_WhenModerationFails`, `PostListing_ReturnsUnauthorized_WithoutToken`, `PostListing_ReturnsBadRequest_WhenCategoryUnknown` (API), `NewListingPage` — *"shows a success message once the listing is published"*, *"shows a rejection message when moderation fails"* (front, avec aperçu en direct de l'annonce avant publication).

### US5 — Consulter le détail d'une annonce

> En tant que **visiteur ou utilisateur**, je veux consulter le détail d'une annonce publiée, afin d'avoir toutes les informations avant un achat éventuel.

- **CA1** : la fiche affiche titre, description, prix, catégorie et nom du vendeur.
- **CA2** : une annonce inexistante ou non publiée renvoie une page "introuvable" (`404`), jamais une erreur technique brute.

**Preuve de conformité** : `GetListingById_ReturnsListing_AfterPublish`, `GetListingById_ReturnsNotFound_WhenUnknown` (API), `ListingDetailPage` — *"shows a not-found message on a 404"* (front).

### US6 — Rechercher une annonce par mot-clé

> En tant que **visiteur**, je veux rechercher une annonce par mot-clé dans son titre, afin de retrouver rapidement un objet précis dans un catalogue qui grossit.

- **CA1** : la recherche filtre les annonces dont le titre contient le mot-clé, sans tenir compte de la casse.
- **CA2** : la recherche se combine avec le filtre de catégorie déjà existant.
- **CA3** : le résultat se met à jour automatiquement pendant la saisie (avec un léger délai pour éviter une requête à chaque frappe).

**Preuve de conformité** : `GetListings_FiltersBySearch_CaseInsensitive` (API), `CatalogPage` — *"refetches listings with the search term after debounce"* (front).

### US7 — Consulter mes propres annonces

> En tant qu'**utilisateur connecté**, je veux consulter la liste de toutes mes annonces (y compris celles rejetées par le contrôle qualité), afin de suivre l'état de mes publications.

- **CA1** : seules mes propres annonces apparaissent, jamais celles d'un autre vendeur.
- **CA2** : contrairement au catalogue public, mes annonces rejetées sont visibles ici, avec un badge de statut explicite.
- **CA3** : je dois être connecté pour accéder à cette page (`401` sinon).

**Preuve de conformité** : `GetMyListings_ReturnsUnauthorized_WithoutToken`, `GetMyListings_ReturnsOwnListings_IncludingRejectedButNotOtherSellers` (API), `MyListingsPage` — *"shows own listings with a status badge"*, *"shows an empty state when the user has no listings"* (front).

## Synthèse de conformité

| User story | Tests d'acceptation associés | Statut |
|---|---|---|
| US1 — Catalogue public | 4 tests (API + front) | ✅ Conforme |
| US2 — Inscription | 5 tests (API + front) | ✅ Conforme |
| US3 — Connexion | 5 tests (API + front) | ✅ Conforme |
| US4 — Publication avec contrôle qualité | 7 tests (API + front) | ✅ Conforme |
| US5 — Détail d'annonce | 5 tests (API + front) | ✅ Conforme |
| US6 — Recherche par mot-clé | 2 tests (API + front) | ✅ Conforme |
| US7 — Mes annonces | 5 tests (API + front) | ✅ Conforme |

L'ensemble de ces critères d'acceptation est couvert par des **tests d'intégration API réels** (via `WebApplicationFactory`, base de données en mémoire, **cookie de session / flow OIDC réellement émis et vérifié** — l'authentification traverse le BFF et l'IdentityServer) et des **tests de composants front** (rendu, interactions utilisateur simulées via `@testing-library/user-event`), exécutés à chaque Pull Request (voir [Processus de test](./02-processus-test.md)).

## Comptes de démonstration (environnement `dev`)

Pour la soutenance, l'environnement `dev` (https://dev.julienforestier.pro) a été peuplé avec trois comptes vendeur et neuf annonces réparties sur les trois catégories, dont deux volontairement rejetées par le contrôle qualité automatique (utile pour démontrer US4/US7 en direct sans dépendre d'une saisie live).

| Email | Mot de passe | Nom affiché | Annonces |
|---|---|---|---|
| `alice@collector.shop` | `Demo1234!` | Alice Vintage | 3 annonces Figurines, toutes publiées |
| `bob@collector.shop` | `Demo1234!` | Bob Sneakers | 2 annonces Sneakers publiées + 1 rejetée (titre trop court, prix négatif) |
| `chloe@collector.shop` | `Demo1234!` | Chloé Vinyles | 2 annonces Vinyles publiées + 1 rejetée (prix hors plage) |

Se connecter avec **Bob** ou **Chloé** puis ouvrir *Mes annonces* permet de montrer en direct qu'une annonce rejetée reste visible pour son auteur (badge "Rejetée") sans jamais apparaître dans le catalogue public — sans avoir à resaisir un scénario d'échec pendant la présentation.
