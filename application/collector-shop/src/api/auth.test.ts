import { afterEach, describe, expect, it, vi } from 'vitest'
import { getUserClaims } from './auth'

describe('auth api', () => {
  afterEach(() => {
    vi.restoreAllMocks()
  })

  it('returns the claims array on a successful session', async () => {
    const claims = [
      { type: 'sub', value: 'user-1' },
      { type: 'email', value: 'a@b.com' },
    ]
    vi.stubGlobal('fetch', vi.fn().mockResolvedValue(new Response(JSON.stringify(claims), { status: 200 })))

    const result = await getUserClaims()

    expect(result).toEqual(claims)
    expect(fetch).toHaveBeenCalledWith(
      '/bff/user',
      expect.objectContaining({ credentials: 'include' }),
    )
  })

  it('returns null when there is no active session (401)', async () => {
    vi.stubGlobal('fetch', vi.fn().mockResolvedValue(new Response(null, { status: 401 })))

    const result = await getUserClaims()

    expect(result).toBeNull()
  })

  it('returns null when the session has no claims (empty array)', async () => {
    vi.stubGlobal('fetch', vi.fn().mockResolvedValue(new Response('[]', { status: 200 })))

    const result = await getUserClaims()

    expect(result).toBeNull()
  })

  it('throws on an unexpected error response', async () => {
    vi.stubGlobal('fetch', vi.fn().mockResolvedValue(new Response(null, { status: 500 })))

    await expect(getUserClaims()).rejects.toThrow()
  })
})
