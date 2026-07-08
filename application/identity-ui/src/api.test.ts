import { afterEach, describe, expect, it, vi } from 'vitest'
import { ApiError, login, register } from './api'

describe('login', () => {
  afterEach(() => {
    vi.restoreAllMocks()
  })

  it('posts credentials to /account/login and returns the returnUrl', async () => {
    const fetchMock = vi.fn().mockResolvedValue(new Response(JSON.stringify({ returnUrl: '/' }), { status: 200 }))
    vi.stubGlobal('fetch', fetchMock)

    const result = await login('a@b.com', 'P@ssword123', '/connect/authorize/callback')

    expect(result).toEqual({ returnUrl: '/' })
    const [url, init] = fetchMock.mock.calls[0]
    expect(url).toBe('/account/login')
    expect(init.method).toBe('POST')
    expect(init.credentials).toBe('include')
    expect(JSON.parse(init.body)).toEqual({
      email: 'a@b.com',
      password: 'P@ssword123',
      returnUrl: '/connect/authorize/callback',
    })
  })

  it('throws an ApiError with the server message on failure', async () => {
    vi.stubGlobal(
      'fetch',
      vi.fn().mockResolvedValue(new Response(JSON.stringify({ message: 'Email ou mot de passe incorrect.' }), { status: 401 })),
    )

    await expect(login('a@b.com', 'wrong', null)).rejects.toMatchObject({
      status: 401,
      message: 'Email ou mot de passe incorrect.',
    })
  })

  it('falls back to statusText when the error body is not JSON', async () => {
    vi.stubGlobal(
      'fetch',
      vi.fn().mockResolvedValue(new Response('not json', { status: 500, statusText: 'Internal Server Error' })),
    )

    await expect(login('a@b.com', 'x', null)).rejects.toMatchObject({
      status: 500,
      message: 'Internal Server Error',
    })
  })
})

describe('register', () => {
  afterEach(() => {
    vi.restoreAllMocks()
  })

  it('posts registration details to /account/register', async () => {
    const fetchMock = vi.fn().mockResolvedValue(new Response(JSON.stringify({ returnUrl: '/' }), { status: 200 }))
    vi.stubGlobal('fetch', fetchMock)

    const result = await register('a@b.com', 'P@ssword123', 'Alice', null)

    expect(result).toEqual({ returnUrl: '/' })
    const [url, init] = fetchMock.mock.calls[0]
    expect(url).toBe('/account/register')
    expect(JSON.parse(init.body)).toEqual({
      email: 'a@b.com',
      password: 'P@ssword123',
      displayName: 'Alice',
      returnUrl: null,
    })
  })

  it('throws an ApiError on conflict', async () => {
    vi.stubGlobal(
      'fetch',
      vi.fn().mockResolvedValue(new Response(JSON.stringify({ message: 'Un compte existe déjà avec cet email.' }), { status: 409 })),
    )

    await expect(register('a@b.com', 'x', 'Alice', null)).rejects.toBeInstanceOf(ApiError)
  })
})
