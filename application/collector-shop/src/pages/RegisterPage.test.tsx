import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MemoryRouter, Route, Routes } from 'react-router-dom'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import { RegisterPage } from './RegisterPage'
import { ApiError } from '../api/client'
import * as AuthContext from '../context/AuthContext'

vi.mock('../context/AuthContext', async () => {
  const actual = await vi.importActual<typeof AuthContext>('../context/AuthContext')
  return { ...actual, useAuth: vi.fn() }
})

function renderPage() {
  return render(
    <MemoryRouter initialEntries={['/inscription']}>
      <Routes>
        <Route path="/inscription" element={<RegisterPage />} />
        <Route path="/" element={<div>Accueil</div>} />
      </Routes>
    </MemoryRouter>,
  )
}

describe('RegisterPage', () => {
  beforeEach(() => {
    vi.restoreAllMocks()
  })

  it('registers and navigates to the catalog on success', async () => {
    const register = vi.fn().mockResolvedValue(undefined)
    vi.mocked(AuthContext.useAuth).mockReturnValue({ user: null, login: vi.fn(), register, logout: vi.fn() })

    renderPage()
    await userEvent.type(screen.getByLabelText('Pseudo'), 'Nouveau Vendeur')
    await userEvent.type(screen.getByLabelText('Adresse email'), 'demo@collector.shop')
    await userEvent.type(screen.getByLabelText('Mot de passe'), 'password123')
    await userEvent.click(screen.getByRole('button', { name: "S'inscrire" }))

    expect(register).toHaveBeenCalledWith('demo@collector.shop', 'password123', 'Nouveau Vendeur')
    expect(await screen.findByText('Accueil')).toBeInTheDocument()
  })

  it('shows a conflict error when the email is already used', async () => {
    const register = vi.fn().mockRejectedValue(new ApiError(409, 'Conflict'))
    vi.mocked(AuthContext.useAuth).mockReturnValue({ user: null, login: vi.fn(), register, logout: vi.fn() })

    renderPage()
    await userEvent.type(screen.getByLabelText('Pseudo'), 'Nouveau Vendeur')
    await userEvent.type(screen.getByLabelText('Adresse email'), 'demo@collector.shop')
    await userEvent.type(screen.getByLabelText('Mot de passe'), 'password123')
    await userEvent.click(screen.getByRole('button', { name: "S'inscrire" }))

    expect(await screen.findByText('Un compte existe déjà avec cet email.')).toBeInTheDocument()
  })
})
