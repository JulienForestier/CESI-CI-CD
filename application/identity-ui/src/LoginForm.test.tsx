import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import { LoginForm } from './LoginForm'
import { ApiError, login } from './api'

vi.mock('./api', async () => {
  const actual = await vi.importActual<typeof import('./api')>('./api')
  return { ...actual, login: vi.fn() }
})

describe('LoginForm', () => {
  beforeEach(() => {
    vi.restoreAllMocks()
    // @ts-expect-error test-only override of a read-only global
    delete window.location
    // @ts-expect-error test-only override of a read-only global
    window.location = { href: '', search: '?returnUrl=%2Fconnect%2Fauthorize%2Fcallback' }
  })

  it('submits the credentials and redirects to the returned returnUrl on success', async () => {
    vi.mocked(login).mockResolvedValue({ returnUrl: '/connect/authorize/callback' })

    render(<LoginForm />)
    await userEvent.type(screen.getByLabelText('Adresse email'), 'a@b.com')
    await userEvent.type(screen.getByLabelText('Mot de passe'), 'P@ssword123')
    await userEvent.click(screen.getByRole('button', { name: 'Se connecter' }))

    expect(login).toHaveBeenCalledWith('a@b.com', 'P@ssword123', '/connect/authorize/callback')
    expect(window.location.href).toBe('/connect/authorize/callback')
  })

  it('shows an invalid credentials message on a 401', async () => {
    vi.mocked(login).mockRejectedValue(new ApiError(401, 'unauthorized'))

    render(<LoginForm />)
    await userEvent.type(screen.getByLabelText('Adresse email'), 'a@b.com')
    await userEvent.type(screen.getByLabelText('Mot de passe'), 'wrong')
    await userEvent.click(screen.getByRole('button', { name: 'Se connecter' }))

    expect(await screen.findByText('Email ou mot de passe incorrect.')).toBeInTheDocument()
  })

  it('shows a generic error message on any other failure', async () => {
    vi.mocked(login).mockRejectedValue(new Error('network down'))

    render(<LoginForm />)
    await userEvent.type(screen.getByLabelText('Adresse email'), 'a@b.com')
    await userEvent.type(screen.getByLabelText('Mot de passe'), 'x')
    await userEvent.click(screen.getByRole('button', { name: 'Se connecter' }))

    expect(await screen.findByText('Une erreur est survenue, réessayez.')).toBeInTheDocument()
  })

  it('links to the register page, preserving the query string', () => {
    render(<LoginForm />)

    expect(screen.getByRole('link', { name: 'Créer un compte' })).toHaveAttribute(
      'href',
      '/register?returnUrl=%2Fconnect%2Fauthorize%2Fcallback',
    )
  })
})
