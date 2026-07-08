import { apiFetch } from './client'
import type { UserProfile } from '../types'

export function getMyProfile() {
  return apiFetch<UserProfile>('/users/me')
}

export function updateDisplayName(displayName: string) {
  return apiFetch<UserProfile>('/users/me', { method: 'PATCH', body: { displayName } })
}
