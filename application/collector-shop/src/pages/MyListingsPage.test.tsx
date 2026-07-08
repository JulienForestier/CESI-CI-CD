import { screen } from '@testing-library/react'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import { MyListingsPage } from './MyListingsPage'
import * as catalogApi from '../api/catalog'
import * as AuthContext from '../context/AuthContext'
import { renderWithProviders } from '../test/renderWithProviders'
import type { Listing } from '../types'

vi.mock('../api/catalog')
vi.mock('../context/AuthContext', async () => {
  const actual = await vi.importActual<typeof AuthContext>('../context/AuthContext')
  return { ...actual, useAuth: vi.fn() }
})

const published: Listing = {
  id: 'listing-1',
  title: 'Annonce publiée',
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

const rejected: Listing = { ...published, id: 'listing-2', title: 'Annonce rejetée', status: 'Rejected' }

describe('MyListingsPage', () => {
  beforeEach(() => {
    vi.restoreAllMocks()
    vi.mocked(AuthContext.useAuth).mockReturnValue({
      user: { token: 'jwt-token', userId: 'seller-1', email: 'a@b.com', displayName: 'Vendeur', isAdmin: false },
      login: vi.fn(),
      register: vi.fn(),
      logout: vi.fn(),
      updateDisplayName: vi.fn(),
    })
  })

  it('shows own listings with a status badge', async () => {
    vi.mocked(catalogApi.getMyListings).mockResolvedValue([published, rejected])

    renderWithProviders(<MyListingsPage />)

    expect(await screen.findByText('Annonce publiée')).toBeInTheDocument()
    expect(screen.getByText('Annonce rejetée')).toBeInTheDocument()
    expect(screen.getByText('Publiée')).toBeInTheDocument()
    expect(screen.getByText('Rejetée')).toBeInTheDocument()
    expect(screen.getByRole('link', { name: /voir l'annonce/i })).toHaveAttribute(
      'href',
      '/annonces/listing-1',
    )
  })

  it('shows an empty state when the user has no listings', async () => {
    vi.mocked(catalogApi.getMyListings).mockResolvedValue([])

    renderWithProviders(<MyListingsPage />)

    expect(await screen.findByText("Vous n'avez publié aucune annonce pour le moment.")).toBeInTheDocument()
  })

  it('shows an error message when the request fails', async () => {
    vi.mocked(catalogApi.getMyListings).mockRejectedValue(new Error('boom'))

    renderWithProviders(<MyListingsPage />)

    expect(await screen.findByText('Impossible de charger vos annonces pour le moment.')).toBeInTheDocument()
  })
})
