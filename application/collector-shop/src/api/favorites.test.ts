import { describe, expect, it, vi } from 'vitest'
import { addFavorite, getFavoriteIds, getFavorites, removeFavorite } from './favorites'
import * as client from './client'

vi.mock('./client')

describe('favorites api', () => {
  it('getFavorites calls /favorites', async () => {
    vi.mocked(client.apiFetch).mockResolvedValue([])

    await getFavorites()

    expect(client.apiFetch).toHaveBeenCalledWith('/favorites')
  })

  it('getFavoriteIds calls /favorites/ids', async () => {
    vi.mocked(client.apiFetch).mockResolvedValue([])

    await getFavoriteIds()

    expect(client.apiFetch).toHaveBeenCalledWith('/favorites/ids')
  })

  it('addFavorite PUTs /listings/:id/favorite', async () => {
    vi.mocked(client.apiFetch).mockResolvedValue(undefined)

    await addFavorite('listing-1')

    expect(client.apiFetch).toHaveBeenCalledWith('/listings/listing-1/favorite', {
      method: 'PUT',
    })
  })

  it('removeFavorite DELETEs /listings/:id/favorite', async () => {
    vi.mocked(client.apiFetch).mockResolvedValue(undefined)

    await removeFavorite('listing-1')

    expect(client.apiFetch).toHaveBeenCalledWith('/listings/listing-1/favorite', {
      method: 'DELETE',
    })
  })
})
