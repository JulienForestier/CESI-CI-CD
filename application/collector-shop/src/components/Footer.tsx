const REASSURANCE_COLUMNS = [
  {
    title: 'Paiement sécurisé',
    description: "Toutes les transactions passent par Collector. Zéro échange d'argent en direct.",
  },
  {
    title: 'Contrôle qualité',
    description: 'Chaque annonce est vérifiée automatiquement avant mise en ligne.',
  },
  {
    title: 'Chat intégré',
    description: 'Discutez avec le vendeur en toute sécurité, sans partage de coordonnées.',
  },
]

export function Footer() {
  return (
    <footer className="bg-ink px-6 py-10 text-card">
      <div className="mx-auto grid max-w-5xl grid-cols-1 gap-8 sm:grid-cols-3">
        {REASSURANCE_COLUMNS.map((column) => (
          <div key={column.title}>
            <div className="mb-1.5 font-display text-[22px] text-gold">{column.title}</div>
            <p className="font-body text-sm leading-relaxed text-muted">{column.description}</p>
          </div>
        ))}
      </div>
    </footer>
  )
}
