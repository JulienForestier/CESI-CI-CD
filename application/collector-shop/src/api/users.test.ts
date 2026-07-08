import { describe, expect, it, vi } from 'vitest'
import { getMyProfile, updateDisplayName } from './users'
import * as client from './client'

vi.mock('./client')

describe('users api', () => {
  it('getMyProfile calls /users/me', async () => {
    vi.mocked(client.apiFetch).mockResolvedValue({})

    await getMyProfile()

    expect(client.apiFetch).toHaveBeenCalledWith('/users/me')
  })

  it('updateDisplayName PATCHes /users/me with the new display name', async () => {
    vi.mocked(client.apiFetch).mockResolvedValue({})

    await updateDisplayName('Nouveau pseudo')

    expect(client.apiFetch).toHaveBeenCalledWith('/users/me', {
      method: 'PATCH',
      body: { displayName: 'Nouveau pseudo' },
    })
  })
})
