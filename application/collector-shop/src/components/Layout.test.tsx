import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MemoryRouter, Route, Routes } from 'react-router-dom'
import { describe, expect, it, vi } from 'vitest'
import { Layout } from './Layout'
import * as AuthContext from '../context/AuthContext'

vi.mock('../context/AuthContext', async () => {
  const actual = await vi.importActual<typeof AuthContext>('../context/AuthContext')
  return { ...actual, useAuth: vi.fn() }
})

function renderLayout() {
  return render(
    <MemoryRouter initialEntries={['/']}>
      <Routes>
        <Route element={<Layout />}>
          <Route index element={<div>Accueil</div>} />
        </Route>
      </Routes>
    </MemoryRouter>,
  )
}

describe('Layout', () => {
  it('shows login/register links when logged out', () => {
    vi.mocked(AuthContext.useAuth).mockReturnValue({
      user: null,
      login: vi.fn(),
      register: vi.fn(),
      logout: vi.fn(),
    })

    renderLayout()

    expect(screen.getByText('Connexion')).toBeInTheDocument()
    expect(screen.getByText('Vendre un objet')).toBeInTheDocument()
  })

  it('shows the user name and logout button when logged in', async () => {
    const logout = vi.fn()
    vi.mocked(AuthContext.useAuth).mockReturnValue({
      user: { token: 't', userId: 'u', email: 'a@b.com', displayName: 'Alice' },
      login: vi.fn(),
      register: vi.fn(),
      logout,
    })

    renderLayout()

    expect(screen.getByText('Alice')).toBeInTheDocument()
    expect(screen.getByText('Mes annonces')).toBeInTheDocument()
    await userEvent.click(screen.getByText('Se déconnecter'))
    expect(logout).toHaveBeenCalled()
  })
})
