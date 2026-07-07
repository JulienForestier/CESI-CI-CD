import { screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import { NewListingPage } from './NewListingPage'
import * as catalogApi from '../api/catalog'
import * as AuthContext from '../context/AuthContext'
import { renderWithProviders } from '../test/renderWithProviders'
import type { Category, Listing } from '../types'

vi.mock('../api/catalog')
vi.mock('../context/AuthContext', async () => {
  const actual = await vi.importActual<typeof AuthContext>('../context/AuthContext')
  return { ...actual, useAuth: vi.fn() }
})

const categories: Category[] = [{ id: 'cat-1', name: 'Figurines' }]

function published(): Listing {
  return {
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
}

describe('NewListingPage', () => {
  beforeEach(() => {
    vi.restoreAllMocks()
    vi.mocked(AuthContext.useAuth).mockReturnValue({
      user: { token: 'jwt-token', userId: 'seller-1', email: 'a@b.com', displayName: 'Vendeur', isAdmin: false },
      login: vi.fn(),
      register: vi.fn(),
      logout: vi.fn(),
    })
    vi.mocked(catalogApi.getCategories).mockResolvedValue(categories)
  })

  it('shows a success message once the listing is published', async () => {
    vi.mocked(catalogApi.createListing).mockResolvedValue(published())

    renderWithProviders(<NewListingPage />)

    await screen.findByText('Figurines')
    await userEvent.type(screen.getByLabelText("Titre de l'annonce"), 'Figurine rare')
    await userEvent.type(screen.getByLabelText('Description'), 'Très bon état')
    await userEvent.type(screen.getByLabelText('Prix (€)'), '42')
    await userEvent.selectOptions(screen.getByLabelText('Catégorie'), 'cat-1')
    await userEvent.click(screen.getByRole('button', { name: "Publier l'annonce" }))

    expect(await screen.findByText('Annonce publiée avec succès !')).toBeInTheDocument()
    expect(catalogApi.createListing).toHaveBeenCalledWith('jwt-token', {
      title: 'Figurine rare',
      description: 'Très bon état',
      price: 42,
      categoryId: 'cat-1',
    })
  })

  it('shows a rejection message when moderation fails', async () => {
    vi.mocked(catalogApi.createListing).mockResolvedValue({ ...published(), status: 'Rejected' })

    renderWithProviders(<NewListingPage />)

    await screen.findByText('Figurines')
    await userEvent.type(screen.getByLabelText("Titre de l'annonce"), 'ab')
    await userEvent.type(screen.getByLabelText('Description'), 'x')
    await userEvent.type(screen.getByLabelText('Prix (€)'), '1')
    await userEvent.selectOptions(screen.getByLabelText('Catégorie'), 'cat-1')
    await userEvent.click(screen.getByRole('button', { name: "Publier l'annonce" }))

    expect(
      await screen.findByText(
        "Votre annonce n'a pas passé le contrôle automatique et n'a pas été publiée. Vérifiez le titre, la description et le prix.",
      ),
    ).toBeInTheDocument()
  })

  it('shows an error message when the request fails', async () => {
    vi.mocked(catalogApi.createListing).mockRejectedValue(new Error('boom'))

    renderWithProviders(<NewListingPage />)

    await screen.findByText('Figurines')
    await userEvent.type(screen.getByLabelText("Titre de l'annonce"), 'Figurine rare')
    await userEvent.type(screen.getByLabelText('Description'), 'Très bon état')
    await userEvent.type(screen.getByLabelText('Prix (€)'), '42')
    await userEvent.selectOptions(screen.getByLabelText('Catégorie'), 'cat-1')
    await userEvent.click(screen.getByRole('button', { name: "Publier l'annonce" }))

    expect(await screen.findByText('Impossible de publier cette annonce pour le moment.')).toBeInTheDocument()
  })

  it('shows validation errors when required fields are missing', async () => {
    renderWithProviders(<NewListingPage />)

    await screen.findByText('Figurines')
    await userEvent.click(screen.getByRole('button', { name: "Publier l'annonce" }))

    expect(await screen.findByText('Le titre est requis')).toBeInTheDocument()
    expect(screen.getByText('La description est requise')).toBeInTheDocument()
    expect(screen.getByText('Choisissez une catégorie')).toBeInTheDocument()
    expect(catalogApi.createListing).not.toHaveBeenCalled()
  })
})
