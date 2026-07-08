import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
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
})
