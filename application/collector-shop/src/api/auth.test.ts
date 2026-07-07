import { describe, expect, it, vi } from 'vitest'
import * as client from './client'
import { login, register } from './auth'

vi.mock('./client')

describe('auth api', () => {
  it('register posts credentials to /auth/register', async () => {
    vi.mocked(client.apiFetch).mockResolvedValue({ token: 't' })

    await register('a@b.com', 'pass', 'Nom')

    expect(client.apiFetch).toHaveBeenCalledWith('/auth/register', {
      method: 'POST',
      body: { email: 'a@b.com', password: 'pass', displayName: 'Nom' },
    })
  })

  it('login posts credentials to /auth/login', async () => {
    vi.mocked(client.apiFetch).mockResolvedValue({ token: 't' })

    await login('a@b.com', 'pass')

    expect(client.apiFetch).toHaveBeenCalledWith('/auth/login', {
      method: 'POST',
      body: { email: 'a@b.com', password: 'pass' },
    })
  })
})
