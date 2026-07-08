import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import * as favoritesApi from '../api/favorites'
import { useAuth } from '../context/AuthContext'

export function useFavoriteIds() {
  const { user } = useAuth()

  return useQuery({
    queryKey: ['favorites', 'ids', user?.userId],
    queryFn: () => favoritesApi.getFavoriteIds(),
    enabled: Boolean(user),
  })
}

export function useFavoriteListings() {
  const { user } = useAuth()

  return useQuery({
    queryKey: ['favorites', 'listings', user?.userId],
    queryFn: () => favoritesApi.getFavorites(),
    enabled: Boolean(user),
  })
}

export function useToggleFavorite() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: ({ listingId, isFavorited }: { listingId: string; isFavorited: boolean }) =>
      isFavorited
        ? favoritesApi.removeFavorite(listingId)
        : favoritesApi.addFavorite(listingId),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['favorites'] })
    },
  })
}
