import { screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import { ModerationPage } from './ModerationPage'
import { ApiError } from '../api/client'
import * as moderationApi from '../api/moderation'
import * as reportsApi from '../api/reports'
import * as AuthContext from '../context/AuthContext'
import { renderWithProviders } from '../test/renderWithProviders'
import type { Listing, Report } from '../types'

vi.mock('../api/moderation')
vi.mock('../api/reports')
vi.mock('../context/AuthContext', async () => {
  const actual = await vi.importActual<typeof AuthContext>('../context/AuthContext')
  return { ...actual, useAuth: vi.fn() }
})

const report: Report = {
  id: 'report-1',
  listingId: 'listing-1',
  listingTitle: 'Vente urgente collection',
  reporterId: 'buyer-1',
  reporterDisplayName: 'Signaleur',
  reason: 'Contenu suspect',
  details: 'Le vendeur demande un paiement hors plateforme.',
  createdAt: new Date().toISOString(),
}

const pendingListing: Listing = {
  id: 'listing-1',
  title: 'Vente urgente collection',
  description: 'Description tout à fait normale et détaillée',
  price: 25,
  status: 'Pending',
  qualityScore: 60,
  moderationReason: 'titre suspect',
  createdAt: new Date().toISOString(),
  sellerId: 'seller-1',
  sellerDisplayName: 'Vendeur Test',
  categoryId: 'cat-1',
  categoryName: 'Figurines',
}

describe('ModerationPage', () => {
  beforeEach(() => {
    vi.restoreAllMocks()
    vi.mocked(AuthContext.useAuth).mockReturnValue({
      isLoading: false,
      user: { userId: 'admin-1', email: 'admin@collector.shop', displayName: 'Admin', isAdmin: true },
      login: vi.fn(),
      register: vi.fn(),
      logout: vi.fn(),
      updateDisplayName: vi.fn(),
    })
    vi.mocked(reportsApi.getReports).mockResolvedValue([])
  })

  it('shows an empty state when there is nothing to moderate', async () => {
    vi.mocked(moderationApi.getPendingListings).mockResolvedValue([])

    renderWithProviders(<ModerationPage />)

    expect(await screen.findByText('Aucune annonce en attente, tout est à jour.')).toBeInTheDocument()
  })

  it('shows an error message when the queue fails to load', async () => {
    vi.mocked(moderationApi.getPendingListings).mockRejectedValue(new Error('boom'))

    renderWithProviders(<ModerationPage />)

    expect(await screen.findByText('Impossible de charger la file de modération.')).toBeInTheDocument()
  })

  it('shows the pending listing with its quality score and reason', async () => {
    vi.mocked(moderationApi.getPendingListings).mockResolvedValue([pendingListing])

    renderWithProviders(<ModerationPage />)

    expect(await screen.findByText('Vente urgente collection')).toBeInTheDocument()
    expect(screen.getByText('60/100 — titre suspect')).toBeInTheDocument()
  })

  it('approves a listing when clicking Valider', async () => {
    vi.mocked(moderationApi.getPendingListings).mockResolvedValue([pendingListing])
    vi.mocked(moderationApi.approveListing).mockResolvedValue({ ...pendingListing, status: 'Published' })

    renderWithProviders(<ModerationPage />)

    await userEvent.click(await screen.findByRole('button', { name: 'Valider' }))

    await waitFor(() => expect(moderationApi.approveListing).toHaveBeenCalledWith('listing-1'))
  })

  it('rejects a listing with a reason when clicking Rejeter then confirming', async () => {
    vi.mocked(moderationApi.getPendingListings).mockResolvedValue([pendingListing])
    vi.mocked(moderationApi.rejectListing).mockResolvedValue({ ...pendingListing, status: 'Rejected' })

    renderWithProviders(<ModerationPage />)

    await userEvent.click(await screen.findByRole('button', { name: 'Rejeter' }))
    await userEvent.type(screen.getByPlaceholderText('Motif du rejet…'), 'Titre non conforme')
    await userEvent.click(screen.getByRole('button', { name: 'Confirmer le rejet' }))

    await waitFor(() =>
      expect(moderationApi.rejectListing).toHaveBeenCalledWith('listing-1', 'Titre non conforme'),
    )
  })

  it('shows an error message when approving fails', async () => {
    vi.mocked(moderationApi.getPendingListings).mockResolvedValue([pendingListing])
    vi.mocked(moderationApi.approveListing).mockRejectedValue(new ApiError(400, "Impossible de valider l'annonce."))

    renderWithProviders(<ModerationPage />)

    await userEvent.click(await screen.findByRole('button', { name: 'Valider' }))

    expect(await screen.findByText("Impossible de valider l'annonce.")).toBeInTheDocument()
  })

  it('shows an empty state when there are no reports', async () => {
    vi.mocked(moderationApi.getPendingListings).mockResolvedValue([])

    renderWithProviders(<ModerationPage />)

    expect(await screen.findByText('Aucun signalement à traiter.')).toBeInTheDocument()
  })

  it('shows a report with its reason and details', async () => {
    vi.mocked(moderationApi.getPendingListings).mockResolvedValue([])
    vi.mocked(reportsApi.getReports).mockResolvedValue([report])

    renderWithProviders(<ModerationPage />)

    expect(await screen.findByText('Contenu suspect')).toBeInTheDocument()
    expect(screen.getByText('Le vendeur demande un paiement hors plateforme.')).toBeInTheDocument()
    expect(screen.getByText(/Signalé par Signaleur/)).toBeInTheDocument()
  })

  it('re-queries reports with the search term', async () => {
    vi.mocked(moderationApi.getPendingListings).mockResolvedValue([])
    vi.mocked(reportsApi.getReports).mockResolvedValue([report])

    renderWithProviders(<ModerationPage />)
    await screen.findByText('Contenu suspect')

    await userEvent.type(screen.getByPlaceholderText('Rechercher dans les motifs…'), 'paiement')

    await waitFor(() => expect(reportsApi.getReports).toHaveBeenCalledWith('paiement'))
  })

  it('shows an error message when reports fail to load', async () => {
    vi.mocked(moderationApi.getPendingListings).mockResolvedValue([])
    vi.mocked(reportsApi.getReports).mockRejectedValue(new Error('boom'))

    renderWithProviders(<ModerationPage />)

    expect(await screen.findByText('Impossible de charger les signalements.')).toBeInTheDocument()
  })
})
