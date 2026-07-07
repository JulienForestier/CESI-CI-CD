import { apiFetch } from './client'
import type { Category, Listing } from '../types'

export function getCategories() {
  return apiFetch<Category[]>('/categories')
}

export function getListings(categoryId?: string) {
  const query = categoryId ? `?categoryId=${categoryId}` : ''
  return apiFetch<Listing[]>(`/listings${query}`)
}

export function getListing(id: string) {
  return apiFetch<Listing>(`/listings/${id}`)
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
