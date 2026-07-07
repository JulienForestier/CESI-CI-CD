import { Link, useParams } from 'react-router-dom'
import { ApiError } from '../api/client'
import { FavoriteButton } from '../components/FavoriteButton'
import { PlaceholderImage } from '../components/PlaceholderImage'
import { useListing } from '../hooks/useCatalog'

const priceFormatter = new Intl.NumberFormat('fr-FR', { style: 'currency', currency: 'EUR' })

export function ListingDetailPage() {
  const { id } = useParams<{ id: string }>()
  const query = useListing(id)

  if (query.isPending) return <p className="font-ui text-brown-2">Chargement…</p>

  if (query.isError) {
    const message =
      query.error instanceof ApiError && query.error.status === 404
        ? 'Cette annonce est introuvable.'
        : "Erreur lors du chargement de l'annonce."
    return <p className="font-ui text-burnt">{message}</p>
  }

  const listing = query.data

  return (
    <div>
      <Link to="/" className="font-ui text-sm font-medium text-burnt hover:underline">
        ← Retour au catalogue
      </Link>

      <div className="mt-5 grid grid-cols-1 gap-9 md:grid-cols-2">
        <PlaceholderImage
          label={`${listing.categoryName.toLowerCase()} · vue principale`}
          className="rounded-xl shadow-[5px_5px_0_rgba(41,33,27,0.15)]"
        />

        <div>
          <div className="mb-2.5 font-ui text-xs font-bold tracking-widest text-burnt uppercase">
            {listing.categoryName}
          </div>
          <h1 className="font-display text-4xl leading-tight">{listing.title}</h1>

          <div className="mt-4 font-display text-4xl">{priceFormatter.format(listing.price)}</div>

          <FavoriteButton listingId={listing.id} variant="button" className="mt-4 w-full" />

          <div className="mt-6 rounded-xl border-[1.5px] border-ink/20 bg-surface p-4">
            <div className="font-ui text-sm font-bold">{listing.sellerDisplayName}</div>
            <div className="font-ui text-xs text-brown-2">Vendeur particulier</div>
          </div>
        </div>
      </div>

      <div className="mt-10 border-t border-ink/10 pt-8">
        <h3 className="mb-3 font-display text-2xl">Description</h3>
        <p className="max-w-2xl font-body text-[15px] leading-relaxed whitespace-pre-line text-brown-1">
          {listing.description}
        </p>
      </div>
    </div>
  )
}
