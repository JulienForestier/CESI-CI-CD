import { screen } from '@testing-library/react'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import { FavoritesPage } from './FavoritesPage'
import * as favoritesApi from '../api/favorites'
import * as AuthContext from '../context/AuthContext'
import { renderWithProviders } from '../test/renderWithProviders'
import type { Listing } from '../types'

vi.mock('../api/favorites')
vi.mock('../context/AuthContext', async () => {
  const actual = await vi.importActual<typeof AuthContext>('../context/AuthContext')
  return { ...actual, useAuth: vi.fn() }
})

const listing: Listing = {
  id: 'listing-1',
  title: 'Figurine favorite',
  description: 'Description',
  price: 42,
  status: 'Published',
  qualityScore: 100,
  moderationReason: 'RAS',
  createdAt: new Date().toISOString(),
  sellerId: 'seller-1',
  sellerDisplayName: 'Vendeur',
  categoryId: 'cat-1',
  categoryName: 'Figurines',
}

describe('FavoritesPage', () => {
  beforeEach(() => {
    vi.restoreAllMocks()
    vi.mocked(AuthContext.useAuth).mockReturnValue({
      user: { token: 'jwt-token', userId: 'user-1', email: 'a@b.com', displayName: 'A', isAdmin: false },
      login: vi.fn(),
      register: vi.fn(),
      logout: vi.fn(),
      updateDisplayName: vi.fn(),
    })
    vi.mocked(favoritesApi.getFavoriteIds).mockResolvedValue(['listing-1'])
  })

  it('shows favorited listings', async () => {
    vi.mocked(favoritesApi.getFavorites).mockResolvedValue([listing])

    renderWithProviders(<FavoritesPage />)

    expect(await screen.findByText('Figurine favorite')).toBeInTheDocument()
  })

  it('shows an empty state when there are no favorites', async () => {
    vi.mocked(favoritesApi.getFavorites).mockResolvedValue([])

    renderWithProviders(<FavoritesPage />)

    expect(
      await screen.findByText(
        "Vous n'avez pas encore ajouté d'annonce à vos favoris. Cliquez sur ♡ sur une annonce pour la retrouver ici.",
      ),
    ).toBeInTheDocument()
  })

  it('shows an error message when the request fails', async () => {
    vi.mocked(favoritesApi.getFavorites).mockRejectedValue(new Error('boom'))

    renderWithProviders(<FavoritesPage />)

    expect(await screen.findByText('Impossible de charger vos favoris pour le moment.')).toBeInTheDocument()
  })
})
