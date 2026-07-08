import { describe, expect, it, vi } from 'vitest'
import { getInterests, updateInterests } from './interests'
import * as client from './client'

vi.mock('./client')

describe('interests api', () => {
  it('getInterests calls /interests', async () => {
    vi.mocked(client.apiFetch).mockResolvedValue([])

    await getInterests()

    expect(client.apiFetch).toHaveBeenCalledWith('/interests')
  })

  it('updateInterests PUTs /interests with the category ids', async () => {
    vi.mocked(client.apiFetch).mockResolvedValue(undefined)

    await updateInterests(['cat-1', 'cat-2'])

    expect(client.apiFetch).toHaveBeenCalledWith('/interests', {
      method: 'PUT',
      body: { categoryIds: ['cat-1', 'cat-2'] },
    })
  })
})
