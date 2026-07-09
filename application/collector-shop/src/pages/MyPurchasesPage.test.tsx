import { screen } from '@testing-library/react'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import { MyPurchasesPage } from './MyPurchasesPage'
import * as purchasesApi from '../api/purchases'
import * as AuthContext from '../context/AuthContext'
import { renderWithProviders } from '../test/renderWithProviders'
import type { Purchase } from '../types'

vi.mock('../api/purchases')
vi.mock('../context/AuthContext', async () => {
  const actual = await vi.importActual<typeof AuthContext>('../context/AuthContext')
  return { ...actual, useAuth: vi.fn() }
})

const purchase: Purchase = {
  id: 'purchase-1',
  listingId: 'listing-1',
  listingTitle: 'Figurine rare',
  buyerId: 'buyer-1',
  sellerId: 'seller-1',
  sellerDisplayName: 'Vendeur',
  price: 42,
  commissionAmount: 2.1,
  createdAt: new Date().toISOString(),
}

describe('MyPurchasesPage', () => {
  beforeEach(() => {
    vi.restoreAllMocks()
    vi.mocked(AuthContext.useAuth).mockReturnValue({
      isLoading: false,
      user: { userId: 'buyer-1', email: 'a@b.com', displayName: 'A', isAdmin: false },
      login: vi.fn(),
      register: vi.fn(),
      logout: vi.fn(),
      updateDisplayName: vi.fn(),
    })
  })

  it('shows purchased listings', async () => {
    vi.mocked(purchasesApi.getMyPurchases).mockResolvedValue([purchase])

    renderWithProviders(<MyPurchasesPage />)

    expect(await screen.findByText('Figurine rare')).toBeInTheDocument()
    expect(screen.getByText('Vendu par Vendeur')).toBeInTheDocument()
    expect(screen.getByText('42,00 €')).toBeInTheDocument()
  })

  it('shows an empty state when there are no purchases', async () => {
    vi.mocked(purchasesApi.getMyPurchases).mockResolvedValue([])

    renderWithProviders(<MyPurchasesPage />)

    expect(
      await screen.findByText("Vous n'avez encore rien acheté sur Collector.shop."),
    ).toBeInTheDocument()
  })

  it('shows an error message when the request fails', async () => {
    vi.mocked(purchasesApi.getMyPurchases).mockRejectedValue(new Error('boom'))

    renderWithProviders(<MyPurchasesPage />)

    expect(
      await screen.findByText('Impossible de charger vos achats pour le moment.'),
    ).toBeInTheDocument()
  })
})
