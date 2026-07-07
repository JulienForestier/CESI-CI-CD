import { apiFetch } from './client'
import type { UserProfile } from '../types'

export function getMyProfile(token: string) {
  return apiFetch<UserProfile>('/users/me', { token })
}

export function updateDisplayName(token: string, displayName: string) {
  return apiFetch<UserProfile>('/users/me', { method: 'PATCH', token, body: { displayName } })
}
