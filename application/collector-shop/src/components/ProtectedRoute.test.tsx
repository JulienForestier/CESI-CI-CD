import { render, screen } from '@testing-library/react'
import { MemoryRouter, Route, Routes } from 'react-router-dom'
import { describe, expect, it, vi } from 'vitest'
import { ProtectedRoute } from './ProtectedRoute'
import * as AuthContext from '../context/AuthContext'

vi.mock('../context/AuthContext', async () => {
  const actual = await vi.importActual<typeof AuthContext>('../context/AuthContext')
  return { ...actual, useAuth: vi.fn() }
})

describe('ProtectedRoute', () => {
  it('redirects to /connexion when there is no authenticated user', () => {
    vi.mocked(AuthContext.useAuth).mockReturnValue({
      isLoading: false,
      user: null,
      login: vi.fn(),
      register: vi.fn(),
      logout: vi.fn(),
      updateDisplayName: vi.fn(),
    })

    render(
      <MemoryRouter initialEntries={['/annonces/nouvelle']}>
        <Routes>
          <Route
            path="/annonces/nouvelle"
            element={
              <ProtectedRoute>
                <div>Contenu protégé</div>
              </ProtectedRoute>
            }
          />
          <Route path="/connexion" element={<div>Page de connexion</div>} />
        </Routes>
      </MemoryRouter>,
    )

    expect(screen.getByText('Page de connexion')).toBeInTheDocument()
  })

  it('renders children when a user is authenticated', () => {
    vi.mocked(AuthContext.useAuth).mockReturnValue({
      isLoading: false,
      user: { userId: 'u', email: 'a@b.com', displayName: 'A', isAdmin: false },
      login: vi.fn(),
      register: vi.fn(),
      logout: vi.fn(),
      updateDisplayName: vi.fn(),
    })

    render(
      <MemoryRouter initialEntries={['/annonces/nouvelle']}>
        <Routes>
          <Route
            path="/annonces/nouvelle"
            element={
              <ProtectedRoute>
                <div>Contenu protégé</div>
              </ProtectedRoute>
            }
          />
        </Routes>
      </MemoryRouter>,
    )

    expect(screen.getByText('Contenu protégé')).toBeInTheDocument()
  })

  it('redirects a non-admin user away from an admin-only route', () => {
    vi.mocked(AuthContext.useAuth).mockReturnValue({
      isLoading: false,
      user: { userId: 'u', email: 'a@b.com', displayName: 'A', isAdmin: false },
      login: vi.fn(),
      register: vi.fn(),
      logout: vi.fn(),
      updateDisplayName: vi.fn(),
    })

    render(
      <MemoryRouter initialEntries={['/admin/moderation']}>
        <Routes>
          <Route
            path="/admin/moderation"
            element={
              <ProtectedRoute adminOnly>
                <div>Contenu admin</div>
              </ProtectedRoute>
            }
          />
          <Route path="/" element={<div>Accueil</div>} />
        </Routes>
      </MemoryRouter>,
    )

    expect(screen.getByText('Accueil')).toBeInTheDocument()
  })

  it('renders children for an admin user on an admin-only route', () => {
    vi.mocked(AuthContext.useAuth).mockReturnValue({
      isLoading: false,
      user: { userId: 'u', email: 'admin@collector.shop', displayName: 'Admin', isAdmin: true },
      login: vi.fn(),
      register: vi.fn(),
      logout: vi.fn(),
      updateDisplayName: vi.fn(),
    })

    render(
      <MemoryRouter initialEntries={['/admin/moderation']}>
        <Routes>
          <Route
            path="/admin/moderation"
            element={
              <ProtectedRoute adminOnly>
                <div>Contenu admin</div>
              </ProtectedRoute>
            }
          />
        </Routes>
      </MemoryRouter>,
    )

    expect(screen.getByText('Contenu admin')).toBeInTheDocument()
  })
})
