import { screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import { InterestsPage } from './InterestsPage'
import { ApiError } from '../api/client'
import * as catalogApi from '../api/catalog'
import * as interestsApi from '../api/interests'
import * as AuthContext from '../context/AuthContext'
import { renderWithProviders } from '../test/renderWithProviders'
import type { Category } from '../types'

vi.mock('../api/catalog')
vi.mock('../api/interests')
vi.mock('../context/AuthContext', async () => {
  const actual = await vi.importActual<typeof AuthContext>('../context/AuthContext')
  return { ...actual, useAuth: vi.fn() }
})

const categories: Category[] = [
  { id: 'cat-1', name: 'Figurines' },
  { id: 'cat-2', name: 'Sneakers' },
]

describe('InterestsPage', () => {
  beforeEach(() => {
    vi.restoreAllMocks()
    vi.mocked(AuthContext.useAuth).mockReturnValue({
      isLoading: false,
      user: { userId: 'user-1', email: 'a@b.com', displayName: 'A', isAdmin: false },
      login: vi.fn(),
      register: vi.fn(),
      logout: vi.fn(),
      updateDisplayName: vi.fn(),
    })
    vi.mocked(catalogApi.getCategories).mockResolvedValue(categories)
  })

  it('renders every category as a selectable card', async () => {
    vi.mocked(interestsApi.getInterests).mockResolvedValue([])

    renderWithProviders(<InterestsPage />)

    expect(await screen.findByText('Figurines')).toBeInTheDocument()
    expect(screen.getByText('Sneakers')).toBeInTheDocument()
    expect(screen.getByRole('button', { name: /Figurines/ })).toHaveAttribute('aria-pressed', 'false')
  })

  it('pre-selects the categories the user is already interested in', async () => {
    vi.mocked(interestsApi.getInterests).mockResolvedValue(['cat-1'])

    renderWithProviders(<InterestsPage />)

    expect(await screen.findByRole('button', { name: /Figurines/ })).toHaveAttribute('aria-pressed', 'true')
    expect(screen.getByRole('button', { name: /Sneakers/ })).toHaveAttribute('aria-pressed', 'false')
  })

  it('toggles a category and saves the selection', async () => {
    vi.mocked(interestsApi.getInterests).mockResolvedValue([])
    vi.mocked(interestsApi.updateInterests).mockResolvedValue(undefined)

    renderWithProviders(<InterestsPage />)

    await userEvent.click(await screen.findByRole('button', { name: /Sneakers/ }))
    await userEvent.click(screen.getByRole('button', { name: 'Enregistrer mes préférences' }))

    await waitFor(() => expect(interestsApi.updateInterests).toHaveBeenCalledWith(['cat-2']))
    expect(await screen.findByText('Vos préférences ont été enregistrées.')).toBeInTheDocument()
  })

  it('shows an error message when saving fails', async () => {
    vi.mocked(interestsApi.getInterests).mockResolvedValue([])
    vi.mocked(interestsApi.updateInterests).mockRejectedValue(new ApiError(400, "Impossible d'enregistrer vos préférences."))

    renderWithProviders(<InterestsPage />)

    await userEvent.click(await screen.findByRole('button', { name: 'Enregistrer mes préférences' }))

    expect(await screen.findByText("Impossible d'enregistrer vos préférences.")).toBeInTheDocument()
  })
})
