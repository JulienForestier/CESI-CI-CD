import { apiFetch } from './client'
import type { Report } from '../types'

export function reportListing(listingId: string, reason: string, details?: string) {
  return apiFetch<Report>(`/listings/${listingId}/report`, { method: 'POST', body: { reason, details } })
}

export function getReports(search?: string) {
  const query = search ? `?search=${encodeURIComponent(search)}` : ''
  return apiFetch<Report[]>(`/admin/reports${query}`)
}
