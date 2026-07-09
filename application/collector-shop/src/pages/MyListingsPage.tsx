import { Link } from 'react-router-dom'
import { PlaceholderImage } from '../components/PlaceholderImage'
import { useMyListings } from '../hooks/useCatalog'
import type { ListingStatus } from '../types'

const priceFormatter = new Intl.NumberFormat('fr-FR', { style: 'currency', currency: 'EUR' })

const STATUS_LABELS: Record<ListingStatus, string> = {
  Published: 'Publiée',
  Rejected: 'Rejetée',
  Pending: 'En attente de modération',
  Sold: 'Vendue',
}

const STATUS_CLASSES: Record<ListingStatus, string> = {
  Published: 'bg-verified text-teal',
  Rejected: 'bg-shipping text-burnt',
  Pending: 'bg-shipping text-burnt',
  Sold: 'bg-ink/10 text-ink',
}

export function MyListingsPage() {
  const listingsQuery = useMyListings()
  const listings = listingsQuery.data ?? []

  return (
    <div>
      <h1 className="mb-8 font-display text-3xl">Mes annonces</h1>

      {listingsQuery.isPending && <p className="font-ui text-brown-2">Chargement…</p>}
      {listingsQuery.isError && (
        <p className="font-ui text-burnt">Impossible de charger vos annonces pour le moment.</p>
      )}
      {listingsQuery.isSuccess && listings.length === 0 && (
        <p className="font-ui text-brown-2">Vous n'avez publié aucune annonce pour le moment.</p>
      )}

      <div className="grid grid-cols-1 gap-5 sm:grid-cols-2 lg:grid-cols-3">
        {listings.map((listing) => (
          <div
            key={listing.id}
            className="overflow-hidden rounded-xl border-[1.5px] border-ink bg-surface shadow-[3px_3px_0_rgba(41,33,27,0.15)]"
          >
            <PlaceholderImage label={`${listing.categoryName.toLowerCase()} · 1/1`} />
            <div className="p-3">
              <span
                className={`mb-2 inline-block rounded-full px-2 py-0.5 font-ui text-[11px] font-semibold ${STATUS_CLASSES[listing.status]}`}
              >
                {STATUS_LABELS[listing.status]}
              </span>
              <div className="mb-0.5 font-ui text-[13px] font-bold text-ink">{listing.title}</div>
              <div className="mb-2 text-xs text-brown-2">{listing.categoryName}</div>
              {listing.status !== 'Published' && listing.status !== 'Sold' && (
                <div className="mb-2 font-ui text-[11px] text-brown-2">{listing.moderationReason}</div>
              )}
              <div className="flex items-center justify-between">
                <span className="font-display text-lg">{priceFormatter.format(listing.price)}</span>
                {listing.status === 'Published' && (
                  <Link
                    to={`/annonces/${listing.id}`}
                    className="font-ui text-[11px] font-semibold text-burnt hover:underline"
                  >
                    Voir l'annonce →
                  </Link>
                )}
              </div>
            </div>
          </div>
        ))}
      </div>
    </div>
  )
}
