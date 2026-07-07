import { describe, expect, it, vi } from 'vitest'
import { getMyProfile, updateDisplayName } from './users'
import * as client from './client'

vi.mock('./client')

describe('users api', () => {
  it('getMyProfile calls /users/me with the bearer token', async () => {
    vi.mocked(client.apiFetch).mockResolvedValue({})

    await getMyProfile('jwt-token')

    expect(client.apiFetch).toHaveBeenCalledWith('/users/me', { token: 'jwt-token' })
  })

  it('updateDisplayName PATCHes /users/me with the new display name', async () => {
    vi.mocked(client.apiFetch).mockResolvedValue({})

    await updateDisplayName('jwt-token', 'Nouveau pseudo')

    expect(client.apiFetch).toHaveBeenCalledWith('/users/me', {
      method: 'PATCH',
      token: 'jwt-token',
      body: { displayName: 'Nouveau pseudo' },
    })
  })
})
