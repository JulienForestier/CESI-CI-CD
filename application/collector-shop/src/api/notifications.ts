import { apiFetch } from './client'
import type { AppNotification } from '../types'

export function getNotifications(token: string) {
  return apiFetch<AppNotification[]>('/notifications', { token })
}

export function markAllNotificationsRead(token: string) {
  return apiFetch<void>('/notifications/mark-all-read', { method: 'POST', token })
}
