import { apiFetch } from './client'
import type { Listing } from '../types'

export function getPendingListings(token: string) {
  return apiFetch<Listing[]>('/admin/listings/pending', { token })
}

export function approveListing(token: string, listingId: string) {
  return apiFetch<Listing>(`/admin/listings/${listingId}/approve`, { method: 'POST', token })
}

export function rejectListing(token: string, listingId: string, reason: string) {
  return apiFetch<Listing>(`/admin/listings/${listingId}/reject`, { method: 'POST', token, body: { reason } })
}
