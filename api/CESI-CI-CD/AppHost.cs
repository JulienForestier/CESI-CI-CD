var builder = DistributedApplication.CreateBuilder(args);

var bffClientSecret = builder.AddParameter("bff-client-secret", secret: true);

var collectorShopDb = builder.AddPostgres("postgres")
    .WithDataVolume()
    .AddDatabase("collectorshop");

// Ports HTTPS fixes (au lieu de laisser Aspire en assigner un dynamiquement) : apiService et
// identityService référencent chacun le endpoint HTTPS de l'AUTRE (IdentityService:Authority /
// Bff:Origin, voir plus bas) — un endpoint alloué dynamiquement ne peut être résolu qu'une fois le
// process réellement démarré, donc deux ressources qui se référencent mutuellement de cette façon
// se bloquent l'une l'autre indéfiniment (ni l'une ni l'autre ne peut démarrer en premier). Un port
// fixe rend la valeur de l'URL connue immédiatement, sans attendre le démarrage du process, et
// casse ce blocage circulaire.
var identityService = builder.AddProject<Projects.CESI_CI_CD_IdentityService>("identityservice")
    .WithHttpsEndpoint(port: 5051, name: "https")
    .WithReference(collectorShopDb)
    .WaitFor(collectorShopDb);

var apiService = builder.AddProject<Projects.CESI_CI_CD_ApiService>("apiservice")
    .WithHttpsEndpoint(port: 5050, name: "https")
    .WithReference(collectorShopDb)
    .WaitFor(collectorShopDb)
    .WithReference(identityService)
    .WaitFor(identityService)
    .WithEnvironment("IdentityService__Authority", identityService.GetEndpoint("https"))
    .WithEnvironment("Bff__ClientSecret", bffClientSecret);

// ApiService a aussi besoin de sa PROPRE origine publique (pour construire ses URIs de callback
// OIDC — redirect_uri, post_logout_redirect_uri — et, en Development, pour sa passerelle interne
// vers IdentityService/le front, voir Program.cs) : exactement le rôle que joue l'ingress k8s en
// prod en exposant une seule origine publique partagée.
apiService.WithEnvironment("Bff__Origin", apiService.GetEndpoint("https"));

identityService
    .WithEnvironment("Bff__Origin", apiService.GetEndpoint("https"))
    .WithEnvironment("Bff__ClientSecret", bffClientSecret);

// Chemin relatif au répertoire d'AppHost.cs (api/CESI-CI-CD/) : il faut remonter deux niveaux
// (api/CESI-CI-CD/ -> api/ -> racine du repo) pour atteindre application/collector-shop, pas un
// seul — une passe à "../application/collector-shop" pointe vers api/application/collector-shop,
// inexistant, et fait échouer le "npm install" au démarrage (ce qui bloquait aussi le reste de
// l'orchestration : apiservice/identityservice n'étaient jamais lancés).
var frontendApp = builder.AddViteApp("app", "../../application/collector-shop")
    .WithReference(apiService);

// Le port du serveur de dev Vite est alloué dynamiquement par Aspire (pas de valeur fixe côté
// vite.config.ts) — ApiService (passerelle locale, voir Program.cs) a donc besoin de le découvrir
// via cette référence plutôt que de supposer un port fixe (5173 par défaut n'est PAS garanti sous
// Aspire, contrairement à "npm run dev" lancé directement).
apiService.WithEnvironment("Frontend__DevServerUrl", frontendApp.GetEndpoint("http"));

await builder.Build().RunAsync();
