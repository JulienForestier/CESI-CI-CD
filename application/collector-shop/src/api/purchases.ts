import { apiFetch } from './client'
import type { Purchase } from '../types'

export function purchaseListing(listingId: string) {
  return apiFetch<Purchase>(`/listings/${listingId}/purchase`, { method: 'POST' })
}

export function getMyPurchases() {
  return apiFetch<Purchase[]>('/purchases/mine')
}
