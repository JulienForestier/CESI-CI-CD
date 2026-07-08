import { afterEach, describe, expect, it, vi } from 'vitest'
import { apiFetch, ApiError } from './client'

describe('apiFetch', () => {
  afterEach(() => {
    vi.restoreAllMocks()
  })

  it('returns parsed JSON on success', async () => {
    vi.stubGlobal(
      'fetch',
      vi.fn().mockResolvedValue(
        new Response(JSON.stringify({ hello: 'world' }), { status: 200 }),
      ),
    )

    const result = await apiFetch<{ hello: string }>('/ping')

    expect(result).toEqual({ hello: 'world' })
    expect(fetch).toHaveBeenCalledWith(
      '/api/ping',
      expect.objectContaining({ method: 'GET' }),
    )
  })

  it('sends credentials and the CSRF header required by Duende.BFF', async () => {
    const fetchMock = vi.fn().mockResolvedValue(new Response('{}', { status: 200 }))
    vi.stubGlobal('fetch', fetchMock)

    await apiFetch('/secure')

    const [, init] = fetchMock.mock.calls[0]
    expect(init.credentials).toBe('include')
    expect((init.headers as Record<string, string>)['X-CSRF']).toBe('1')
  })

  it('returns undefined for a 204 response', async () => {
    vi.stubGlobal('fetch', vi.fn().mockResolvedValue(new Response(null, { status: 204 })))

    const result = await apiFetch('/nothing')

    expect(result).toBeUndefined()
  })

  it('throws an ApiError with the server message on failure', async () => {
    vi.stubGlobal(
      'fetch',
      vi.fn().mockResolvedValue(
        new Response(JSON.stringify({ message: 'Nope' }), { status: 400 }),
      ),
    )

    await expect(apiFetch('/fail')).rejects.toMatchObject(
      new ApiError(400, 'Nope'),
    )
  })

  it('falls back to statusText when the error body is not JSON', async () => {
    vi.stubGlobal(
      'fetch',
      vi.fn().mockResolvedValue(new Response('not json', { status: 500, statusText: 'Server Error' })),
    )

    await expect(apiFetch('/fail')).rejects.toMatchObject({ status: 500, message: 'Server Error' })
  })
})
