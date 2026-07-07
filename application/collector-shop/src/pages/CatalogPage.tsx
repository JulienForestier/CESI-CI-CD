import { useState } from 'react'
import { ListingCard } from '../components/ListingCard'
import { useCategories, useListings } from '../hooks/useCatalog'

export function CatalogPage() {
  const [selectedCategory, setSelectedCategory] = useState('')
  const categoriesQuery = useCategories()
  const listingsQuery = useListings(selectedCategory || undefined)

  const categories = categoriesQuery.data ?? []
  const listings = listingsQuery.data ?? []

  return (
    <div>
      <div className="mb-8 flex items-end justify-between">
        <div>
          <h1 className="font-display text-3xl">Catalogue</h1>
          {listingsQuery.isSuccess && (
            <p className="mt-1 font-ui text-sm text-brown-2">{listings.length} résultat(s)</p>
          )}
        </div>
        <select
          aria-label="Filtrer par catégorie"
          value={selectedCategory}
          onChange={(event) => setSelectedCategory(event.target.value)}
          className="rounded-lg border-[1.5px] border-ink bg-surface px-3.5 py-2 font-ui text-sm font-semibold"
        >
          <option value="">Toutes les catégories</option>
          {categories.map((category) => (
            <option key={category.id} value={category.id}>
              {category.name}
            </option>
          ))}
        </select>
      </div>

      {listingsQuery.isPending && <p className="font-ui text-brown-2">Chargement…</p>}
      {listingsQuery.isError && (
        <p className="font-ui text-burnt">Impossible de charger le catalogue pour le moment.</p>
      )}
      {listingsQuery.isSuccess && listings.length === 0 && (
        <p className="font-ui text-brown-2">Aucune annonce pour le moment.</p>
      )}

      <div className="grid grid-cols-1 gap-5 sm:grid-cols-2 lg:grid-cols-3">
        {listings.map((listing) => (
          <ListingCard key={listing.id} listing={listing} />
        ))}
      </div>
    </div>
  )
}
