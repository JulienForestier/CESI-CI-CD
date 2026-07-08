import { describe, expect, it, vi } from 'vitest'
import { getNotifications, markAllNotificationsRead } from './notifications'
import * as client from './client'

vi.mock('./client')

describe('notifications api', () => {
  it('getNotifications calls /notifications', async () => {
    vi.mocked(client.apiFetch).mockResolvedValue([])

    await getNotifications()

    expect(client.apiFetch).toHaveBeenCalledWith('/notifications')
  })

  it('markAllNotificationsRead POSTs /notifications/mark-all-read', async () => {
    vi.mocked(client.apiFetch).mockResolvedValue(undefined)

    await markAllNotificationsRead()

    expect(client.apiFetch).toHaveBeenCalledWith('/notifications/mark-all-read', {
      method: 'POST',
    })
  })
})
