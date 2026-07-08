import { apiFetch } from './client'
import type { Listing } from '../types'

export function getPendingListings() {
  return apiFetch<Listing[]>('/admin/listings/pending')
}

export function approveListing(listingId: string) {
  return apiFetch<Listing>(`/admin/listings/${listingId}/approve`, { method: 'POST' })
}

export function rejectListing(listingId: string, reason: string) {
  return apiFetch<Listing>(`/admin/listings/${listingId}/reject`, { method: 'POST', body: { reason } })
}
