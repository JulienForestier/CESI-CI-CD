import { Link } from 'react-router-dom'
import { PlaceholderImage } from './PlaceholderImage'
import type { Listing } from '../types'

const priceFormatter = new Intl.NumberFormat('fr-FR', { style: 'currency', currency: 'EUR' })

export function ListingCard({ listing }: { listing: Listing }) {
  return (
    <Link
      to={`/annonces/${listing.id}`}
      className="block overflow-hidden rounded-xl border-[1.5px] border-ink bg-surface shadow-[3px_3px_0_rgba(41,33,27,0.15)] transition hover:-translate-y-0.5"
    >
      <PlaceholderImage label={`${listing.categoryName.toLowerCase()} · 1/1`} />
      <div className="p-3">
        <div className="mb-0.5 font-ui text-[13px] font-bold text-ink">{listing.title}</div>
        <div className="mb-2 text-xs text-brown-2">{listing.categoryName}</div>
        <div className="flex items-center justify-between">
          <span className="font-display text-lg">{priceFormatter.format(listing.price)}</span>
          <span className="font-ui text-[11px] text-brown-2">{listing.sellerDisplayName}</span>
        </div>
      </div>
    </Link>
  )
}
