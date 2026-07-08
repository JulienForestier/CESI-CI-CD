import { apiFetch } from './client'
import type { Listing } from '../types'

export function getFavorites(token: string) {
  return apiFetch<Listing[]>('/favorites', { token })
}

export function getFavoriteIds(token: string) {
  return apiFetch<string[]>('/favorites/ids', { token })
}

export function addFavorite(token: string, listingId: string) {
  return apiFetch<void>(`/listings/${listingId}/favorite`, { method: 'PUT', token })
}

export function removeFavorite(token: string, listingId: string) {
  return apiFetch<void>(`/listings/${listingId}/favorite`, { method: 'DELETE', token })
}
