import { render, screen, waitFor } from '@testing-library/react'
import { QueryClientProvider } from '@tanstack/react-query'
import userEvent from '@testing-library/user-event'
import { MemoryRouter, Route, Routes } from 'react-router-dom'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import { ProfilePage } from './ProfilePage'
import { ApiError } from '../api/client'
import * as catalogApi from '../api/catalog'
import * as favoritesApi from '../api/favorites'
import * as usersApi from '../api/users'
import * as AuthContext from '../context/AuthContext'
import { createTestQueryClient } from '../test/queryClient'
import type { Listing, UserProfile } from '../types'

vi.mock('../api/users')
vi.mock('../api/catalog')
vi.mock('../api/favorites')
vi.mock('../context/AuthContext', async () => {
  const actual = await vi.importActual<typeof AuthContext>('../context/AuthContext')
  return { ...actual, useAuth: vi.fn() }
})

const profile: UserProfile = {
  id: 'user-1',
  email: 'buyer@collector.shop',
  displayName: 'Buyer Demo',
  isAdmin: false,
  createdAt: '2026-01-15T10:00:00.000Z',
}

function renderProfilePage() {
  return render(
    <QueryClientProvider client={createTestQueryClient()}>
      <MemoryRouter initialEntries={['/profil']}>
        <Routes>
          <Route path="/profil" element={<ProfilePage />} />
          <Route path="/" element={<div>Accueil</div>} />
        </Routes>
      </MemoryRouter>
    </QueryClientProvider>,
  )
}

describe('ProfilePage', () => {
  const logout = vi.fn()
  const updateDisplayName = vi.fn()

  beforeEach(() => {
    vi.restoreAllMocks()
    vi.mocked(AuthContext.useAuth).mockReturnValue({
      isLoading: false,
      user: { userId: 'user-1', email: 'buyer@collector.shop', displayName: 'Buyer Demo', isAdmin: false },
      login: vi.fn(),
      register: vi.fn(),
      logout,
      updateDisplayName,
    })
    vi.mocked(catalogApi.getMyListings).mockResolvedValue([{ id: 'listing-1' } as Listing])
    vi.mocked(favoritesApi.getFavorites).mockResolvedValue([])
  })

  it('shows a loading state while the profile is fetched', () => {
    vi.mocked(usersApi.getMyProfile).mockReturnValue(new Promise(() => {}))

    renderProfilePage()

    expect(screen.getByText('Chargement…')).toBeInTheDocument()
  })

  it('shows an error message when the profile fails to load', async () => {
    vi.mocked(usersApi.getMyProfile).mockRejectedValue(new Error('boom'))

    renderProfilePage()

    expect(await screen.findByText('Impossible de charger votre profil pour le moment.')).toBeInTheDocument()
  })

  it('renders the profile information and stats', async () => {
    vi.mocked(usersApi.getMyProfile).mockResolvedValue(profile)

    renderProfilePage()

    expect(await screen.findByText('Buyer Demo')).toBeInTheDocument()
    expect(screen.getByText('buyer@collector.shop')).toBeInTheDocument()
    expect(screen.getByText('15 janvier 2026')).toBeInTheDocument()
    expect(screen.getByText('Acheteur · Vendeur')).toBeInTheDocument()
    expect(screen.queryByText('Administrateur')).not.toBeInTheDocument()
    await waitFor(() => expect(screen.getByText('Annonces publiées').nextSibling).toHaveTextContent('1'))
    expect(screen.getByText('Favoris').nextSibling).toHaveTextContent('0')
  })

  it('shows the admin badge for admin users', async () => {
    vi.mocked(usersApi.getMyProfile).mockResolvedValue({ ...profile, isAdmin: true })

    renderProfilePage()

    expect(await screen.findByText('Administrateur')).toBeInTheDocument()
  })

  it('updates the display name and syncs the auth context', async () => {
    vi.mocked(usersApi.getMyProfile).mockResolvedValue(profile)
    vi.mocked(usersApi.updateDisplayName).mockResolvedValue({ ...profile, displayName: 'Nouveau pseudo' })

    renderProfilePage()

    const input = await screen.findByLabelText('Pseudo')
    await userEvent.clear(input)
    await userEvent.type(input, 'Nouveau pseudo')
    await userEvent.click(screen.getByRole('button', { name: 'Enregistrer' }))

    await waitFor(() => expect(usersApi.updateDisplayName).toHaveBeenCalledWith('Nouveau pseudo'))
    expect(updateDisplayName).toHaveBeenCalledWith('Nouveau pseudo')
    expect(await screen.findByText('Votre pseudo a été mis à jour.')).toBeInTheDocument()
  })

  it('shows an error message when updating the display name fails', async () => {
    vi.mocked(usersApi.getMyProfile).mockResolvedValue(profile)
    vi.mocked(usersApi.updateDisplayName).mockRejectedValue(new ApiError(400, 'Le pseudo ne peut pas être vide.'))

    renderProfilePage()

    await screen.findByLabelText('Pseudo')
    await userEvent.click(screen.getByRole('button', { name: 'Enregistrer' }))

    expect(await screen.findByText('Le pseudo ne peut pas être vide.')).toBeInTheDocument()
  })

  it('calls logout when clicking "Se déconnecter"', async () => {
    vi.mocked(usersApi.getMyProfile).mockResolvedValue(profile)

    renderProfilePage()

    await userEvent.click(await screen.findByRole('button', { name: 'Se déconnecter' }))

    expect(logout).toHaveBeenCalled()
  })
})
