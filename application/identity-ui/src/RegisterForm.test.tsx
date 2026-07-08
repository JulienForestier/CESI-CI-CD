import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import { RegisterForm } from './RegisterForm'
import { ApiError, register } from './api'

vi.mock('./api', async () => {
  const actual = await vi.importActual<typeof import('./api')>('./api')
  return { ...actual, register: vi.fn() }
})

describe('RegisterForm', () => {
  beforeEach(() => {
    vi.restoreAllMocks()
    // @ts-expect-error test-only override of a read-only global
    delete window.location
    // @ts-expect-error test-only override of a read-only global
    window.location = { href: '', search: '' }
  })

  it('submits the registration details and redirects on success', async () => {
    vi.mocked(register).mockResolvedValue({ returnUrl: '/' })

    render(<RegisterForm />)
    await userEvent.type(screen.getByLabelText('Pseudo'), 'Alice')
    await userEvent.type(screen.getByLabelText('Adresse email'), 'a@b.com')
    await userEvent.type(screen.getByLabelText('Mot de passe'), 'P@ssword123')
    await userEvent.click(screen.getByRole('button', { name: "S'inscrire" }))

    expect(register).toHaveBeenCalledWith('a@b.com', 'P@ssword123', 'Alice', null)
    expect(window.location.href).toBe('/')
  })

  it('shows a conflict message when the email is already registered', async () => {
    vi.mocked(register).mockRejectedValue(new ApiError(409, 'conflict'))

    render(<RegisterForm />)
    await userEvent.type(screen.getByLabelText('Pseudo'), 'Alice')
    await userEvent.type(screen.getByLabelText('Adresse email'), 'a@b.com')
    await userEvent.type(screen.getByLabelText('Mot de passe'), 'P@ssword123')
    await userEvent.click(screen.getByRole('button', { name: "S'inscrire" }))

    expect(await screen.findByText('Un compte existe déjà avec cet email.')).toBeInTheDocument()
  })

  it('shows the server validation message on a 400', async () => {
    vi.mocked(register).mockRejectedValue(new ApiError(400, 'Le mot de passe doit contenir au moins 8 caractères.'))

    render(<RegisterForm />)
    await userEvent.type(screen.getByLabelText('Pseudo'), 'Alice')
    await userEvent.type(screen.getByLabelText('Adresse email'), 'a@b.com')
    await userEvent.type(screen.getByLabelText('Mot de passe'), 'short')
    await userEvent.click(screen.getByRole('button', { name: "S'inscrire" }))

    expect(await screen.findByText('Le mot de passe doit contenir au moins 8 caractères.')).toBeInTheDocument()
  })

  it('shows a generic error message on an unexpected failure', async () => {
    vi.mocked(register).mockRejectedValue(new Error('network down'))

    render(<RegisterForm />)
    await userEvent.type(screen.getByLabelText('Pseudo'), 'Alice')
    await userEvent.type(screen.getByLabelText('Adresse email'), 'a@b.com')
    await userEvent.type(screen.getByLabelText('Mot de passe'), 'P@ssword123')
    await userEvent.click(screen.getByRole('button', { name: "S'inscrire" }))

    expect(await screen.findByText('Une erreur est survenue, réessayez.')).toBeInTheDocument()
  })

  it('links to the login page, preserving the query string', () => {
    window.location.search = '?returnUrl=%2Fconnect%2Fauthorize%2Fcallback'
    render(<RegisterForm />)

    expect(screen.getByRole('link', { name: 'Se connecter' })).toHaveAttribute(
      'href',
      '/login?returnUrl=%2Fconnect%2Fauthorize%2Fcallback',
    )
  })
})
