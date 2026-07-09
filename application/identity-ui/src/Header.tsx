// Version statique du Header de collector-shop : ni navigation ni état utilisateur (on est
// toujours déconnecté sur cette page) — un <a> classique suffit, identity-ui n'a pas de routeur
// et "/" doit de toute façon recharger la page pour revenir sur l'app collector-shop.
export function Header() {
  return (
    <header className="border-b border-ink/10 bg-card">
      <div className="mx-auto flex max-w-5xl items-center gap-2.5 px-6 py-4">
        <a href="/" className="flex shrink-0 items-center gap-2.5">
          <span className="flex h-[38px] w-[38px] items-center justify-center rounded-full border-2 border-ink font-display text-[20px] leading-none">
            C
          </span>
          <span className="font-display text-[23px] tracking-[-0.01em]">
            Collector<span className="text-burnt">.shop</span>
          </span>
        </a>
      </div>
    </header>
  )
}
