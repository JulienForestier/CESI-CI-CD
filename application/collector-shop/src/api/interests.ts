import { apiFetch } from './client'

export function getInterests(token: string) {
  return apiFetch<string[]>('/interests', { token })
}

export function updateInterests(token: string, categoryIds: string[]) {
  return apiFetch<void>('/interests', { method: 'PUT', token, body: { categoryIds } })
}
