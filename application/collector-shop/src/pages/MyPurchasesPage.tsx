import { useMyPurchases } from '../hooks/usePurchases'

const priceFormatter = new Intl.NumberFormat('fr-FR', { style: 'currency', currency: 'EUR' })
const dateFormatter = new Intl.DateTimeFormat('fr-FR', { dateStyle: 'medium', timeStyle: 'short' })

export function MyPurchasesPage() {
  const purchasesQuery = useMyPurchases()
  const purchases = purchasesQuery.data ?? []

  return (
    <div>
      <h1 className="mb-8 font-display text-3xl">Mes achats</h1>

      {purchasesQuery.isPending && <p className="font-ui text-brown-2">Chargement…</p>}
      {purchasesQuery.isError && (
        <p className="font-ui text-burnt">Impossible de charger vos achats pour le moment.</p>
      )}
      {purchasesQuery.isSuccess && purchases.length === 0 && (
        <p className="font-ui text-brown-2">Vous n'avez encore rien acheté sur Collector.shop.</p>
      )}

      <div className="flex flex-col gap-3">
        {purchases.map((purchase) => (
          <div
            key={purchase.id}
            className="rounded-xl border-[1.5px] border-ink bg-surface p-4 shadow-[3px_3px_0_rgba(41,33,27,0.15)]"
          >
            <div className="flex items-start justify-between gap-3">
              <div>
                <div className="font-ui text-sm font-bold text-ink">{purchase.listingTitle}</div>
                <div className="mt-0.5 font-ui text-xs text-brown-2">
                  Vendu par {purchase.sellerDisplayName}
                </div>
                <div className="mt-2 font-ui text-[11px] text-brown-2">
                  Acheté le {dateFormatter.format(new Date(purchase.createdAt))}
                </div>
              </div>
              <span className="font-display text-lg whitespace-nowrap">
                {priceFormatter.format(purchase.price)}
              </span>
            </div>
          </div>
        ))}
      </div>
    </div>
  )
}
