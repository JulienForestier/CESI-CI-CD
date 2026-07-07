import { screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import { CatalogPage } from './CatalogPage'
import * as catalogApi from '../api/catalog'
import { renderWithProviders } from '../test/renderWithProviders'
import type { Category, Listing } from '../types'

vi.mock('../api/catalog')

const categories: Category[] = [
  { id: 'cat-1', name: 'Figurines' },
  { id: 'cat-2', name: 'Vinyles' },
]

const listings: Listing[] = [
  {
    id: 'listing-1',
    title: 'Figurine rare',
    description: 'Description',
    price: 10,
    status: 'Published',
    createdAt: new Date().toISOString(),
    sellerId: 'seller-1',
    sellerDisplayName: 'Vendeur',
    categoryId: 'cat-1',
    categoryName: 'Figurines',
  },
]

describe('CatalogPage', () => {
  beforeEach(() => {
    vi.restoreAllMocks()
  })

  it('shows the catalog once categories and listings are loaded', async () => {
    vi.mocked(catalogApi.getCategories).mockResolvedValue(categories)
    vi.mocked(catalogApi.getListings).mockResolvedValue(listings)

    renderWithProviders(<CatalogPage />)

    expect(await screen.findByText('Figurine rare')).toBeInTheDocument()
    expect(screen.getByText('Toutes les catégories')).toBeInTheDocument()
  })

  it('shows an empty state when there are no listings', async () => {
    vi.mocked(catalogApi.getCategories).mockResolvedValue([])
    vi.mocked(catalogApi.getListings).mockResolvedValue([])

    renderWithProviders(<CatalogPage />)

    expect(await screen.findByText('Aucune annonce pour le moment.')).toBeInTheDocument()
  })

  it('shows an error message when the catalog fails to load', async () => {
    vi.mocked(catalogApi.getCategories).mockResolvedValue([])
    vi.mocked(catalogApi.getListings).mockRejectedValue(new Error('network error'))

    renderWithProviders(<CatalogPage />)

    expect(await screen.findByText('Impossible de charger le catalogue pour le moment.')).toBeInTheDocument()
  })

  it('refetches listings when the category filter changes', async () => {
    vi.mocked(catalogApi.getCategories).mockResolvedValue(categories)
    vi.mocked(catalogApi.getListings).mockResolvedValue(listings)

    renderWithProviders(<CatalogPage />)

    await screen.findByText('Figurine rare')
    await userEvent.selectOptions(screen.getByLabelText('Filtrer par catégorie'), 'cat-1')

    await waitFor(() =>
      expect(catalogApi.getListings).toHaveBeenCalledWith({ categoryId: 'cat-1', search: undefined }),
    )
  })

  it('refetches listings with the search term after debounce', async () => {
    vi.mocked(catalogApi.getCategories).mockResolvedValue(categories)
    vi.mocked(catalogApi.getListings).mockResolvedValue(listings)

    renderWithProviders(<CatalogPage />)

    await screen.findByText('Figurine rare')
    await userEvent.type(screen.getByLabelText('Rechercher une annonce'), 'goku')

    await waitFor(
      () => expect(catalogApi.getListings).toHaveBeenCalledWith({ categoryId: undefined, search: 'goku' }),
      { timeout: 1000 },
    )
  })
})
