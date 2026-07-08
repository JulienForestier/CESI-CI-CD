import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import { LoginPage } from './LoginPage'
import * as AuthContext from '../context/AuthContext'

vi.mock('../context/AuthContext', async () => {
  const actual = await vi.importActual<typeof AuthContext>('../context/AuthContext')
  return { ...actual, useAuth: vi.fn() }
})

describe('LoginPage', () => {
  beforeEach(() => {
    vi.restoreAllMocks()
  })

  it('redirects to the identity service on mount', () => {
    const login = vi.fn()
    vi.mocked(AuthContext.useAuth).mockReturnValue({
      isLoading: false,
      user: null,
      login,
      register: vi.fn(),
      logout: vi.fn(),
      updateDisplayName: vi.fn(),
    })

    render(<LoginPage />)

    expect(login).toHaveBeenCalled()
  })

  it('also redirects when the fallback button is clicked', async () => {
    const login = vi.fn()
    vi.mocked(AuthContext.useAuth).mockReturnValue({
      isLoading: false,
      user: null,
      login,
      register: vi.fn(),
      logout: vi.fn(),
      updateDisplayName: vi.fn(),
    })

    render(<LoginPage />)
    login.mockClear()
    await userEvent.click(screen.getByRole('button', { name: 'Continuer' }))

    expect(login).toHaveBeenCalled()
  })
})
