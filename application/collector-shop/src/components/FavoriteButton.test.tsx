import { render, screen, waitFor } from '@testing-library/react'
import { QueryClientProvider } from '@tanstack/react-query'
import userEvent from '@testing-library/user-event'
import { MemoryRouter, Route, Routes } from 'react-router-dom'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import { FavoriteButton } from './FavoriteButton'
import * as favoritesApi from '../api/favorites'
import * as AuthContext from '../context/AuthContext'
import { createTestQueryClient } from '../test/queryClient'
import { renderWithProviders } from '../test/renderWithProviders'

vi.mock('../api/favorites')
vi.mock('../context/AuthContext', async () => {
  const actual = await vi.importActual<typeof AuthContext>('../context/AuthContext')
  return { ...actual, useAuth: vi.fn() }
})

describe('FavoriteButton', () => {
  beforeEach(() => {
    vi.restoreAllMocks()
  })

  it('navigates to /connexion when clicked while logged out', async () => {
    vi.mocked(AuthContext.useAuth).mockReturnValue({
      user: null,
      login: vi.fn(),
      register: vi.fn(),
      logout: vi.fn(),
    })

    render(
      <QueryClientProvider client={createTestQueryClient()}>
        <MemoryRouter initialEntries={['/']}>
          <Routes>
            <Route path="/" element={<FavoriteButton listingId="listing-1" />} />
            <Route path="/connexion" element={<div>Page de connexion</div>} />
          </Routes>
        </MemoryRouter>
      </QueryClientProvider>,
    )
    await userEvent.click(screen.getByRole('button'))

    expect(await screen.findByText('Page de connexion')).toBeInTheDocument()
  })

  it('shows an outline heart and adds the listing to favorites when clicked', async () => {
    vi.mocked(AuthContext.useAuth).mockReturnValue({
      user: { token: 'jwt-token', userId: 'user-1', email: 'a@b.com', displayName: 'A' },
      login: vi.fn(),
      register: vi.fn(),
      logout: vi.fn(),
    })
    vi.mocked(favoritesApi.getFavoriteIds).mockResolvedValue([])
    vi.mocked(favoritesApi.addFavorite).mockResolvedValue(undefined)

    renderWithProviders(<FavoriteButton listingId="listing-1" />)

    expect(await screen.findByText('♡')).toBeInTheDocument()
    await userEvent.click(screen.getByRole('button'))

    await waitFor(() => expect(favoritesApi.addFavorite).toHaveBeenCalledWith('jwt-token', 'listing-1'))
  })

  it('shows a filled heart and removes the listing from favorites when clicked', async () => {
    vi.mocked(AuthContext.useAuth).mockReturnValue({
      user: { token: 'jwt-token', userId: 'user-1', email: 'a@b.com', displayName: 'A' },
      login: vi.fn(),
      register: vi.fn(),
      logout: vi.fn(),
    })
    vi.mocked(favoritesApi.getFavoriteIds).mockResolvedValue(['listing-1'])
    vi.mocked(favoritesApi.removeFavorite).mockResolvedValue(undefined)

    renderWithProviders(<FavoriteButton listingId="listing-1" />)

    expect(await screen.findByText('♥')).toBeInTheDocument()
    await userEvent.click(screen.getByRole('button'))

    await waitFor(() => expect(favoritesApi.removeFavorite).toHaveBeenCalledWith('jwt-token', 'listing-1'))
  })

  it('renders a labeled button in the "button" variant', async () => {
    vi.mocked(AuthContext.useAuth).mockReturnValue({
      user: { token: 'jwt-token', userId: 'user-1', email: 'a@b.com', displayName: 'A' },
      login: vi.fn(),
      register: vi.fn(),
      logout: vi.fn(),
    })
    vi.mocked(favoritesApi.getFavoriteIds).mockResolvedValue([])

    renderWithProviders(<FavoriteButton listingId="listing-1" variant="button" />)

    expect(await screen.findByText('♡ Ajouter aux favoris')).toBeInTheDocument()
  })
})
