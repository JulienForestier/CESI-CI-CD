import { describe, expect, it, vi } from 'vitest'
import { getInterests, updateInterests } from './interests'
import * as client from './client'

vi.mock('./client')

describe('interests api', () => {
  it('getInterests calls /interests with the bearer token', async () => {
    vi.mocked(client.apiFetch).mockResolvedValue([])

    await getInterests('jwt-token')

    expect(client.apiFetch).toHaveBeenCalledWith('/interests', { token: 'jwt-token' })
  })

  it('updateInterests PUTs /interests with the category ids', async () => {
    vi.mocked(client.apiFetch).mockResolvedValue(undefined)

    await updateInterests('jwt-token', ['cat-1', 'cat-2'])

    expect(client.apiFetch).toHaveBeenCalledWith('/interests', {
      method: 'PUT',
      token: 'jwt-token',
      body: { categoryIds: ['cat-1', 'cat-2'] },
    })
  })
})
