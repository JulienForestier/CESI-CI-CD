import { apiFetch } from './client'
import type { AppNotification } from '../types'

export function getNotifications() {
  return apiFetch<AppNotification[]>('/notifications')
}

export function markAllNotificationsRead() {
  return apiFetch<void>('/notifications/mark-all-read', { method: 'POST' })
}
