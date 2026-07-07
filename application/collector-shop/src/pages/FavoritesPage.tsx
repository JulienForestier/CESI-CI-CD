import { ListingCard } from '../components/ListingCard'
import { useFavoriteListings } from '../hooks/useFavorites'

export function FavoritesPage() {
  const favoritesQuery = useFavoriteListings()
  const listings = favoritesQuery.data ?? []

  return (
    <div>
      <h1 className="mb-8 font-display text-3xl">Mes favoris</h1>

      {favoritesQuery.isPending && <p className="font-ui text-brown-2">Chargement…</p>}
      {favoritesQuery.isError && (
        <p className="font-ui text-burnt">Impossible de charger vos favoris pour le moment.</p>
      )}
      {favoritesQuery.isSuccess && listings.length === 0 && (
        <p className="font-ui text-brown-2">
          Vous n'avez pas encore ajouté d'annonce à vos favoris. Cliquez sur ♡ sur une annonce pour la
          retrouver ici.
        </p>
      )}

      <div className="grid grid-cols-1 gap-5 sm:grid-cols-2 lg:grid-cols-3">
        {listings.map((listing) => (
          <ListingCard key={listing.id} listing={listing} />
        ))}
      </div>
    </div>
  )
}
