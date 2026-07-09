import { render, screen, waitFor } from '@testing-library/react'
import { QueryClientProvider } from '@tanstack/react-query'
import userEvent from '@testing-library/user-event'
import { MemoryRouter, Route, Routes } from 'react-router-dom'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import { ListingDetailPage } from './ListingDetailPage'
import { ApiError } from '../api/client'
import * as authApi from '../api/auth'
import * as catalogApi from '../api/catalog'
import * as chatApi from '../api/chat'
import * as reportsApi from '../api/reports'
import * as purchasesApi from '../api/purchases'
import { AuthProvider } from '../context/AuthContext'
import { createTestQueryClient } from '../test/queryClient'
import type { Listing } from '../types'

vi.mock('../api/auth')
vi.mock('../api/catalog')
vi.mock('../api/chat')
vi.mock('../api/reports')
vi.mock('../api/purchases')

const listing: Listing = {
  id: 'listing-1',
  title: 'Figurine rare',
  description: 'Très bon état',
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

function loginAs(userId: string, displayName: string) {
  vi.mocked(authApi.getUserClaims).mockResolvedValue([
    { type: 'sub', value: userId },
    { type: 'email', value: `${userId}@collector.shop` },
    { type: 'name', value: displayName },
  ])
}

function loginAsBuyer() {
  loginAs('buyer-1', 'Buyer')
}

function renderAt(id: string) {
  return render(
    <QueryClientProvider client={createTestQueryClient()}>
      <MemoryRouter initialEntries={[`/annonces/${id}`]}>
        <AuthProvider>
          <Routes>
            <Route path="/annonces/:id" element={<ListingDetailPage />} />
            <Route path="/connexion" element={<div>Page de connexion</div>} />
            <Route path="/messages/:conversationId" element={<div>Fil de conversation</div>} />
            <Route path="/mes-achats" element={<div>Mes achats</div>} />
          </Routes>
        </AuthProvider>
      </MemoryRouter>
    </QueryClientProvider>,
  )
}

describe('ListingDetailPage', () => {
  beforeEach(() => {
    vi.restoreAllMocks()
    vi.mocked(authApi.getUserClaims).mockResolvedValue(null)
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

  it('navigates to /connexion when contacting the seller while logged out', async () => {
    vi.mocked(catalogApi.getListing).mockResolvedValue(listing)

    renderAt('listing-1')

    await userEvent.click(await screen.findByRole('button', { name: '💬 Discuter avec le vendeur' }))

    expect(await screen.findByText('Page de connexion')).toBeInTheDocument()
  })

  it('starts a conversation and navigates to the thread when logged in', async () => {
    loginAsBuyer()
    vi.mocked(catalogApi.getListing).mockResolvedValue(listing)
    vi.mocked(chatApi.startConversation).mockResolvedValue({ id: 'conv-1' })

    renderAt('listing-1')

    await userEvent.click(await screen.findByRole('button', { name: '💬 Discuter avec le vendeur' }))

    expect(await screen.findByText('Fil de conversation')).toBeInTheDocument()
    expect(chatApi.startConversation).toHaveBeenCalledWith('listing-1')
  })

  it('shows an error message when starting the conversation fails', async () => {
    loginAsBuyer()
    vi.mocked(catalogApi.getListing).mockResolvedValue(listing)
    vi.mocked(chatApi.startConversation).mockRejectedValue(
      new ApiError(400, 'Vous ne pouvez pas démarrer une conversation avec vous-même.'),
    )

    renderAt('listing-1')

    await userEvent.click(await screen.findByRole('button', { name: '💬 Discuter avec le vendeur' }))

    await waitFor(() =>
      expect(
        screen.getByText('Vous ne pouvez pas démarrer une conversation avec vous-même.'),
      ).toBeInTheDocument(),
    )
  })

  it('hides the contact-seller and purchase buttons for the listing owner', async () => {
    loginAs('seller-1', 'Vendeur')
    vi.mocked(catalogApi.getListing).mockResolvedValue(listing)

    renderAt('listing-1')

    await screen.findByText('Figurine rare')
    expect(screen.queryByRole('button', { name: '💬 Discuter avec le vendeur' })).not.toBeInTheDocument()
    expect(screen.queryByRole('button', { name: '🛒 Acheter' })).not.toBeInTheDocument()
  })

  it('navigates to /connexion when buying while logged out', async () => {
    vi.mocked(catalogApi.getListing).mockResolvedValue(listing)

    renderAt('listing-1')

    await userEvent.click(await screen.findByRole('button', { name: '🛒 Acheter' }))

    expect(await screen.findByText('Page de connexion')).toBeInTheDocument()
  })

  it('purchases the listing and navigates to /mes-achats when logged in', async () => {
    loginAsBuyer()
    vi.mocked(catalogApi.getListing).mockResolvedValue(listing)
    vi.mocked(purchasesApi.purchaseListing).mockResolvedValue({
      id: 'purchase-1',
      listingId: 'listing-1',
      listingTitle: 'Figurine rare',
      buyerId: 'buyer-1',
      sellerId: 'seller-1',
      sellerDisplayName: 'Vendeur',
      price: 42,
      commissionAmount: 2.1,
      createdAt: new Date().toISOString(),
    })

    renderAt('listing-1')

    await userEvent.click(await screen.findByRole('button', { name: '🛒 Acheter' }))

    expect(await screen.findByText('Mes achats')).toBeInTheDocument()
    expect(purchasesApi.purchaseListing).toHaveBeenCalledWith('listing-1')
  })

  it('shows an error message when the purchase fails', async () => {
    loginAsBuyer()
    vi.mocked(catalogApi.getListing).mockResolvedValue(listing)
    vi.mocked(purchasesApi.purchaseListing).mockRejectedValue(
      new ApiError(409, "Cette annonce n'est plus disponible à l'achat."),
    )

    renderAt('listing-1')

    await userEvent.click(await screen.findByRole('button', { name: '🛒 Acheter' }))

    await waitFor(() =>
      expect(screen.getByText("Cette annonce n'est plus disponible à l'achat.")).toBeInTheDocument(),
    )
  })

  it('hides the report link for logged-out visitors', async () => {
    vi.mocked(catalogApi.getListing).mockResolvedValue(listing)

    renderAt('listing-1')

    await screen.findByText('Figurine rare')
    expect(screen.queryByText('🚩 Signaler cette annonce')).not.toBeInTheDocument()
  })

  it('hides the report link for the listing owner', async () => {
    loginAs('seller-1', 'Vendeur')
    vi.mocked(catalogApi.getListing).mockResolvedValue(listing)

    renderAt('listing-1')

    await screen.findByText('Figurine rare')
    expect(screen.queryByText('🚩 Signaler cette annonce')).not.toBeInTheDocument()
  })

  it('submits a report and shows a confirmation message', async () => {
    loginAsBuyer()
    vi.mocked(catalogApi.getListing).mockResolvedValue(listing)
    vi.mocked(reportsApi.reportListing).mockResolvedValue({
      id: 'report-1',
      listingId: 'listing-1',
      listingTitle: listing.title,
      reporterId: 'buyer-1',
      reporterDisplayName: 'Buyer',
      reason: 'Contenu suspect',
      details: null,
      createdAt: new Date().toISOString(),
    })

    renderAt('listing-1')

    await userEvent.click(await screen.findByText('🚩 Signaler cette annonce'))
    await userEvent.type(screen.getByPlaceholderText('Motif du signalement…'), 'Contenu suspect')
    await userEvent.click(screen.getByRole('button', { name: 'Envoyer le signalement' }))

    expect(await screen.findByText('Signalement envoyé, merci.')).toBeInTheDocument()
    expect(reportsApi.reportListing).toHaveBeenCalledWith('listing-1', 'Contenu suspect', undefined)
  })

  it('shows an error message when reporting fails', async () => {
    loginAsBuyer()
    vi.mocked(catalogApi.getListing).mockResolvedValue(listing)
    vi.mocked(reportsApi.reportListing).mockRejectedValue(new ApiError(409, 'Vous avez déjà signalé cette annonce.'))

    renderAt('listing-1')

    await userEvent.click(await screen.findByText('🚩 Signaler cette annonce'))
    await userEvent.type(screen.getByPlaceholderText('Motif du signalement…'), 'Contenu suspect')
    await userEvent.click(screen.getByRole('button', { name: 'Envoyer le signalement' }))

    expect(await screen.findByText('Vous avez déjà signalé cette annonce.')).toBeInTheDocument()
  })
})
