import { apiFetch } from './client'

export function getInterests() {
  return apiFetch<string[]>('/interests')
}

export function updateInterests(categoryIds: string[]) {
  return apiFetch<void>('/interests', { method: 'PUT', body: { categoryIds } })
}
