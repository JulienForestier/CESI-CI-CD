import { render, screen } from '@testing-library/react'
import { beforeEach, describe, expect, it } from 'vitest'
import { App } from './App'

describe('App', () => {
  beforeEach(() => {
    // @ts-expect-error test-only override of a read-only global
    delete window.location
    // @ts-expect-error test-only override of a read-only global
    window.location = { pathname: '/login', search: '' }
  })

  it('renders the login form by default', () => {
    window.location.pathname = '/login'
    render(<App />)
    expect(screen.getByRole('heading', { name: 'Bon retour parmi nous' })).toBeInTheDocument()
  })

  it('renders the register form when the path starts with /register', () => {
    window.location.pathname = '/register'
    render(<App />)
    expect(screen.getByRole('heading', { name: 'Créer un compte' })).toBeInTheDocument()
  })
})
