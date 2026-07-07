import { render, screen } from '@testing-library/react'
import { QueryClientProvider } from '@tanstack/react-query'
import { MemoryRouter, Route, Routes } from 'react-router-dom'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import { ListingDetailPage } from './ListingDetailPage'
import { ApiError } from '../api/client'
import * as catalogApi from '../api/catalog'
import { createTestQueryClient } from '../test/queryClient'
import type { Listing } from '../types'

vi.mock('../api/catalog')

const listing: Listing = {
  id: 'listing-1',
  title: 'Figurine rare',
  description: 'Très bon état',
  price: 42,
  status: 'Published',
  createdAt: new Date().toISOString(),
  sellerId: 'seller-1',
  sellerDisplayName: 'Vendeur',
  categoryId: 'cat-1',
  categoryName: 'Figurines',
}

function renderAt(id: string) {
  return render(
    <QueryClientProvider client={createTestQueryClient()}>
      <MemoryRouter initialEntries={[`/annonces/${id}`]}>
        <Routes>
          <Route path="/annonces/:id" element={<ListingDetailPage />} />
        </Routes>
      </MemoryRouter>
    </QueryClientProvider>,
  )
}

describe('ListingDetailPage', () => {
  beforeEach(() => {
    vi.restoreAllMocks()
  })

  it('renders the listing once loaded', async () => {
    vi.mocked(catalogApi.getListing).mockResolvedValue(listing)

    renderAt('listing-1')

    expect(await screen.findByText('Figurine rare')).toBeInTheDocument()
    expect(screen.getByText('42,00 €')).toBeInTheDocument()
    expect(screen.getByText('Vendeur')).toBeInTheDocument()
  })

  it('shows a not-found message on a 404', async () => {
    vi.mocked(catalogApi.getListing).mockRejectedValue(new ApiError(404, 'Not found'))

    renderAt('missing')

    expect(await screen.findByText('Cette annonce est introuvable.')).toBeInTheDocument()
  })

  it('shows a generic error message on other failures', async () => {
    vi.mocked(catalogApi.getListing).mockRejectedValue(new Error('boom'))

    renderAt('listing-1')

    expect(await screen.findByText("Erreur lors du chargement de l'annonce.")).toBeInTheDocument()
  })
})
