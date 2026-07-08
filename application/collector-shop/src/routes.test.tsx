import { render, screen } from '@testing-library/react'
import { QueryClientProvider } from '@tanstack/react-query'
import { createMemoryRouter, createRoutesFromElements, RouterProvider } from 'react-router-dom'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import * as catalogApi from './api/catalog'
import * as AuthContext from './context/AuthContext'
import { routeElements } from './routes'
import { createTestQueryClient } from './test/queryClient'

vi.mock('./api/catalog')
vi.mock('./context/AuthContext', async () => {
  const actual = await vi.importActual<typeof AuthContext>('./context/AuthContext')
  return { ...actual, useAuth: vi.fn() }
})

function renderAt(path: string) {
  const router = createMemoryRouter(createRoutesFromElements(routeElements), { initialEntries: [path] })
  return render(
    <QueryClientProvider client={createTestQueryClient()}>
      <RouterProvider router={router} />
    </QueryClientProvider>,
  )
}

describe('App routing', () => {
  beforeEach(() => {
    vi.restoreAllMocks()
    vi.mocked(catalogApi.getCategories).mockResolvedValue([])
    vi.mocked(catalogApi.getListings).mockResolvedValue([])
    vi.mocked(AuthContext.useAuth).mockReturnValue({
      isLoading: false,
      user: null,
      login: vi.fn(),
      register: vi.fn(),
      logout: vi.fn(),
      updateDisplayName: vi.fn(),
    })
  })

  it.each([
    ['/', 'Catalogue'],
    ['/connexion', 'Bon retour parmi nous'],
    ['/inscription', 'Créer un compte'],
  ])('renders the "%s" heading directly at %s', async (path, heading) => {
    renderAt(path)

    expect(await screen.findByRole('heading', { name: heading })).toBeInTheDocument()
  })

  it.each(['/annonces/nouvelle', '/mes-annonces'])(
    'redirects to /connexion when visiting %s while logged out',
    async (path) => {
      renderAt(path)

      expect(await screen.findByRole('heading', { name: 'Bon retour parmi nous' })).toBeInTheDocument()
    },
  )

  it('renders my listings when logged in', async () => {
    vi.mocked(AuthContext.useAuth).mockReturnValue({
      isLoading: false,
      user: { userId: 'seller-1', email: 'a@b.com', displayName: 'Alice', isAdmin: false },
      login: vi.fn(),
      register: vi.fn(),
      logout: vi.fn(),
      updateDisplayName: vi.fn(),
    })
    vi.mocked(catalogApi.getMyListings).mockResolvedValue([])

    renderAt('/mes-annonces')

    expect(await screen.findByRole('heading', { name: 'Mes annonces' })).toBeInTheDocument()
  })
})
