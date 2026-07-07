import { describe, expect, it, vi } from 'vitest'
import { addFavorite, getFavoriteIds, getFavorites, removeFavorite } from './favorites'
import * as client from './client'

vi.mock('./client')

describe('favorites api', () => {
  it('getFavorites calls /favorites with the bearer token', async () => {
    vi.mocked(client.apiFetch).mockResolvedValue([])

    await getFavorites('jwt-token')

    expect(client.apiFetch).toHaveBeenCalledWith('/favorites', { token: 'jwt-token' })
  })

  it('getFavoriteIds calls /favorites/ids with the bearer token', async () => {
    vi.mocked(client.apiFetch).mockResolvedValue([])

    await getFavoriteIds('jwt-token')

    expect(client.apiFetch).toHaveBeenCalledWith('/favorites/ids', { token: 'jwt-token' })
  })

  it('addFavorite PUTs /listings/:id/favorite', async () => {
    vi.mocked(client.apiFetch).mockResolvedValue(undefined)

    await addFavorite('jwt-token', 'listing-1')

    expect(client.apiFetch).toHaveBeenCalledWith('/listings/listing-1/favorite', {
      method: 'PUT',
      token: 'jwt-token',
    })
  })

  it('removeFavorite DELETEs /listings/:id/favorite', async () => {
    vi.mocked(client.apiFetch).mockResolvedValue(undefined)

    await removeFavorite('jwt-token', 'listing-1')

    expect(client.apiFetch).toHaveBeenCalledWith('/listings/listing-1/favorite', {
      method: 'DELETE',
      token: 'jwt-token',
    })
  })
})
