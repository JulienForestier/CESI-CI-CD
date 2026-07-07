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
      user: null,
      login: vi.fn(),
      register: vi.fn(),
      logout: vi.fn(),
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
      user: { token: 't', userId: 'u', email: 'a@b.com', displayName: 'A' },
      login: vi.fn(),
      register: vi.fn(),
      logout: vi.fn(),
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
})
