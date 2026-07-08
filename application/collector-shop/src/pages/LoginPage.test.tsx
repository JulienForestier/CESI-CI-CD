import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MemoryRouter, Route, Routes } from 'react-router-dom'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import { LoginPage } from './LoginPage'
import { ApiError } from '../api/client'
import * as AuthContext from '../context/AuthContext'

vi.mock('../context/AuthContext', async () => {
  const actual = await vi.importActual<typeof AuthContext>('../context/AuthContext')
  return { ...actual, useAuth: vi.fn() }
})

function renderPage() {
  return render(
    <MemoryRouter initialEntries={['/connexion']}>
      <Routes>
        <Route path="/connexion" element={<LoginPage />} />
        <Route path="/" element={<div>Accueil</div>} />
      </Routes>
    </MemoryRouter>,
  )
}

describe('LoginPage', () => {
  beforeEach(() => {
    vi.restoreAllMocks()
  })

  it('logs in and navigates to the catalog on success', async () => {
    const login = vi.fn().mockResolvedValue(undefined)
    vi.mocked(AuthContext.useAuth).mockReturnValue({ user: null, login, register: vi.fn(), logout: vi.fn(), updateDisplayName: vi.fn() })

    renderPage()
    await userEvent.type(screen.getByLabelText('Adresse email'), 'demo@collector.shop')
    await userEvent.type(screen.getByLabelText('Mot de passe'), 'password123')
    await userEvent.click(screen.getByRole('button', { name: 'Se connecter' }))

    expect(login).toHaveBeenCalledWith('demo@collector.shop', 'password123')
    expect(await screen.findByText('Accueil')).toBeInTheDocument()
  })

  it('shows an error message on invalid credentials', async () => {
    const login = vi.fn().mockRejectedValue(new ApiError(401, 'Unauthorized'))
    vi.mocked(AuthContext.useAuth).mockReturnValue({ user: null, login, register: vi.fn(), logout: vi.fn(), updateDisplayName: vi.fn() })

    renderPage()
    await userEvent.type(screen.getByLabelText('Adresse email'), 'demo@collector.shop')
    await userEvent.type(screen.getByLabelText('Mot de passe'), 'wrong')
    await userEvent.click(screen.getByRole('button', { name: 'Se connecter' }))

    expect(await screen.findByText('Email ou mot de passe incorrect.')).toBeInTheDocument()
  })

  it('shows a generic error message on unexpected failures', async () => {
    const login = vi.fn().mockRejectedValue(new Error('boom'))
    vi.mocked(AuthContext.useAuth).mockReturnValue({ user: null, login, register: vi.fn(), logout: vi.fn(), updateDisplayName: vi.fn() })

    renderPage()
    await userEvent.type(screen.getByLabelText('Adresse email'), 'demo@collector.shop')
    await userEvent.type(screen.getByLabelText('Mot de passe'), 'wrong')
    await userEvent.click(screen.getByRole('button', { name: 'Se connecter' }))

    expect(await screen.findByText('Une erreur est survenue, réessayez.')).toBeInTheDocument()
  })
})
