import { apiFetch } from './client'
import type { Listing } from '../types'

export function getFavorites() {
  return apiFetch<Listing[]>('/favorites')
}

export function getFavoriteIds() {
  return apiFetch<string[]>('/favorites/ids')
}

export function addFavorite(listingId: string) {
  return apiFetch<void>(`/listings/${listingId}/favorite`, { method: 'PUT' })
}

export function removeFavorite(listingId: string) {
  return apiFetch<void>(`/listings/${listingId}/favorite`, { method: 'DELETE' })
}
