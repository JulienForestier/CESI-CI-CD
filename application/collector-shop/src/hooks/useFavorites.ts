import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import * as favoritesApi from '../api/favorites'
import { useAuth } from '../context/AuthContext'

export function useFavoriteIds() {
  const { user } = useAuth()

  return useQuery({
    queryKey: ['favorites', 'ids', user?.userId],
    queryFn: () => favoritesApi.getFavoriteIds(user!.token),
    enabled: Boolean(user),
  })
}

export function useFavoriteListings() {
  const { user } = useAuth()

  return useQuery({
    queryKey: ['favorites', 'listings', user?.userId],
    queryFn: () => favoritesApi.getFavorites(user!.token),
    enabled: Boolean(user),
  })
}

export function useToggleFavorite() {
  const { user } = useAuth()
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: ({ listingId, isFavorited }: { listingId: string; isFavorited: boolean }) =>
      isFavorited
        ? favoritesApi.removeFavorite(user!.token, listingId)
        : favoritesApi.addFavorite(user!.token, listingId),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['favorites'] })
    },
  })
}
