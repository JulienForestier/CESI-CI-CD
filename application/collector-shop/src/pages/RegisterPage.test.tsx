import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MemoryRouter, Route, Routes } from 'react-router-dom'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import { RegisterPage } from './RegisterPage'
import * as AuthContext from '../context/AuthContext'

vi.mock('../context/AuthContext', async () => {
  const actual = await vi.importActual<typeof AuthContext>('../context/AuthContext')
  return { ...actual, useAuth: vi.fn() }
})

describe('RegisterPage', () => {
  beforeEach(() => {
    vi.restoreAllMocks()
  })

  it('redirects to the identity service on mount', () => {
    const register = vi.fn()
    vi.mocked(AuthContext.useAuth).mockReturnValue({
      isLoading: false,
      user: null,
      login: vi.fn(),
      register,
      logout: vi.fn(),
      updateDisplayName: vi.fn(),
    })

    render(<RegisterPage />)

    expect(register).toHaveBeenCalled()
    expect(screen.getByRole('status', { name: 'Redirection en cours' })).toBeInTheDocument()
  })

  it('also redirects when the fallback button is clicked', async () => {
    const register = vi.fn()
    vi.mocked(AuthContext.useAuth).mockReturnValue({
      isLoading: false,
      user: null,
      login: vi.fn(),
      register,
      logout: vi.fn(),
      updateDisplayName: vi.fn(),
    })

    render(<RegisterPage />)
    register.mockClear()
    await userEvent.click(screen.getByRole('button', { name: 'Continuer' }))

    expect(register).toHaveBeenCalled()
  })

  it('does not redirect to the identity service when already authenticated', () => {
    const register = vi.fn()
    vi.mocked(AuthContext.useAuth).mockReturnValue({
      isLoading: false,
      user: { userId: '1', email: 'a@b.com', displayName: 'A', isAdmin: false },
      login: vi.fn(),
      register,
      logout: vi.fn(),
      updateDisplayName: vi.fn(),
    })

    render(
      <MemoryRouter initialEntries={['/inscription']}>
        <Routes>
          <Route path="/inscription" element={<RegisterPage />} />
          <Route path="/" element={<div>Home</div>} />
        </Routes>
      </MemoryRouter>,
    )

    expect(register).not.toHaveBeenCalled()
    expect(screen.getByText('Home')).toBeInTheDocument()
  })
})
