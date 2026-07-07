import { render, screen } from '@testing-library/react'
import { QueryClientProvider } from '@tanstack/react-query'
import { MemoryRouter } from 'react-router-dom'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import App from './App'
import * as catalogApi from './api/catalog'
import * as AuthContext from './context/AuthContext'
import { createTestQueryClient } from './test/queryClient'

vi.mock('./api/catalog')
vi.mock('./context/AuthContext', async () => {
  const actual = await vi.importActual<typeof AuthContext>('./context/AuthContext')
  return { ...actual, useAuth: vi.fn() }
})

function renderAt(path: string) {
  return render(
    <QueryClientProvider client={createTestQueryClient()}>
      <MemoryRouter initialEntries={[path]}>
        <App />
      </MemoryRouter>
    </QueryClientProvider>,
  )
}

describe('App routing', () => {
  beforeEach(() => {
    vi.restoreAllMocks()
    vi.mocked(catalogApi.getCategories).mockResolvedValue([])
    vi.mocked(catalogApi.getListings).mockResolvedValue([])
    vi.mocked(AuthContext.useAuth).mockReturnValue({
      user: null,
      login: vi.fn(),
      register: vi.fn(),
      logout: vi.fn(),
    })
  })

  it('renders the catalog on the index route', async () => {
    renderAt('/')

    expect(await screen.findByRole('heading', { name: 'Catalogue' })).toBeInTheDocument()
  })

  it('renders the login page on /connexion', async () => {
    renderAt('/connexion')

    expect(await screen.findByRole('heading', { name: 'Bon retour parmi nous' })).toBeInTheDocument()
  })

  it('renders the register page on /inscription', async () => {
    renderAt('/inscription')

    expect(await screen.findByRole('heading', { name: 'Créer un compte' })).toBeInTheDocument()
  })

  it('redirects to /connexion when visiting the new listing page while logged out', async () => {
    renderAt('/annonces/nouvelle')

    expect(await screen.findByRole('heading', { name: 'Bon retour parmi nous' })).toBeInTheDocument()
  })

  it('redirects to /connexion when visiting my listings while logged out', async () => {
    renderAt('/mes-annonces')

    expect(await screen.findByRole('heading', { name: 'Bon retour parmi nous' })).toBeInTheDocument()
  })

  it('renders my listings when logged in', async () => {
    vi.mocked(AuthContext.useAuth).mockReturnValue({
      user: { token: 't', userId: 'seller-1', email: 'a@b.com', displayName: 'Alice', isAdmin: false },
      login: vi.fn(),
      register: vi.fn(),
      logout: vi.fn(),
    })
    vi.mocked(catalogApi.getMyListings).mockResolvedValue([])

    renderAt('/mes-annonces')

    expect(await screen.findByRole('heading', { name: 'Mes annonces' })).toBeInTheDocument()
  })
})
