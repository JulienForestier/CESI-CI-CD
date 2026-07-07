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
- **CA3** : en cas de succès, je reçois un jeton d'authentification et suis redirigé vers le catalogue.

**Preuve de conformité** : `Register_CreatesUser_AndReturnsToken`, `Register_ReturnsConflict_WhenEmailAlreadyUsed`, `Register_ReturnsBadRequest_WhenFieldMissing` (API), `RegisterPage` — *"registers and navigates to the catalog on success"*, *"shows a conflict error when the email is already used"* (front).

### US3 — Se connecter

> En tant qu'**utilisateur inscrit**, je veux me connecter avec mes identifiants, afin de retrouver mon espace.

- **CA1** : identifiants valides → jeton retourné.
- **CA2** : email inconnu **ou** mot de passe incorrect → `401` avec un message générique identique dans les deux cas (pas d'énumération des comptes existants).

**Preuve de conformité** : `Login_ReturnsToken_WithValidCredentials`, `Login_ReturnsUnauthorized_WhenUserUnknown`, `Login_ReturnsUnauthorized_WhenPasswordWrong` (API), `LoginPage` — *"logs in and navigates to the catalog on success"*, *"shows an error message on invalid credentials"* (front).

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

## Synthèse de conformité

| User story | Tests d'acceptation associés | Statut |
|---|---|---|
| US1 — Catalogue public | 4 tests (API + front) | ✅ Conforme |
| US2 — Inscription | 5 tests (API + front) | ✅ Conforme |
| US3 — Connexion | 5 tests (API + front) | ✅ Conforme |
| US4 — Publication avec contrôle qualité | 7 tests (API + front) | ✅ Conforme |
| US5 — Détail d'annonce | 5 tests (API + front) | ✅ Conforme |

L'ensemble de ces critères d'acceptation est couvert par des **tests d'intégration API réels** (via `WebApplicationFactory`, base de données en mémoire, JWT réellement émis et vérifié) et des **tests de composants front** (rendu, interactions utilisateur simulées via `@testing-library/user-event`), exécutés à chaque Pull Request (voir [Processus de test](./02-processus-test.md)).
