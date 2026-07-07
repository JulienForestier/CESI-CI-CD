import { describe, expect, it, vi } from 'vitest'
import { getNotifications, markAllNotificationsRead } from './notifications'
import * as client from './client'

vi.mock('./client')

describe('notifications api', () => {
  it('getNotifications calls /notifications with the bearer token', async () => {
    vi.mocked(client.apiFetch).mockResolvedValue([])

    await getNotifications('jwt-token')

    expect(client.apiFetch).toHaveBeenCalledWith('/notifications', { token: 'jwt-token' })
  })

  it('markAllNotificationsRead POSTs /notifications/mark-all-read', async () => {
    vi.mocked(client.apiFetch).mockResolvedValue(undefined)

    await markAllNotificationsRead('jwt-token')

    expect(client.apiFetch).toHaveBeenCalledWith('/notifications/mark-all-read', {
      method: 'POST',
      token: 'jwt-token',
    })
  })
})
