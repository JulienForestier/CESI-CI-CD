import { apiFetch } from './client'
import type { Category, Listing } from '../types'

export function getCategories() {
  return apiFetch<Category[]>('/categories')
}

export interface ListingsFilter {
  categoryId?: string
  search?: string
}

export function getListings(filter: ListingsFilter = {}) {
  const params = new URLSearchParams()
  if (filter.categoryId) params.set('categoryId', filter.categoryId)
  if (filter.search) params.set('search', filter.search)
  const query = params.toString()
  return apiFetch<Listing[]>(`/listings${query ? `?${query}` : ''}`)
}

export function getListing(id: string) {
  return apiFetch<Listing>(`/listings/${id}`)
}

export function getMyListings(token: string) {
  return apiFetch<Listing[]>('/listings/mine', { token })
}

export interface CreateListingInput {
  title: string
  description: string
  price: number
  categoryId: string
}

export function createListing(token: string, input: CreateListingInput) {
  return apiFetch<Listing>('/listings', { method: 'POST', body: input, token })
}
