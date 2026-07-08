import { act, renderHook, waitFor } from '@testing-library/react'
import { QueryClientProvider } from '@tanstack/react-query'
import type { ReactNode } from 'react'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import * as authApi from '../api/auth'
import type { BffClaim } from '../api/auth'
import { createTestQueryClient } from '../test/queryClient'
import { AuthProvider, useAuth } from './AuthContext'

vi.mock('../api/auth')

const claims: BffClaim[] = [
  { type: 'sub', value: 'user-1' },
  { type: 'email', value: 'demo@collector.shop' },
  { type: 'name', value: 'Demo' },
  { type: 'bff:logout_url', value: '/bff/logout?sid=abc' },
]

const adminClaims: BffClaim[] = [...claims, { type: 'role', value: 'Admin' }]

function wrapper({ children }: { children: ReactNode }) {
  return (
    <QueryClientProvider client={createTestQueryClient()}>
      <AuthProvider>{children}</AuthProvider>
    </QueryClientProvider>
  )
}

describe('AuthContext', () => {
  beforeEach(() => {
    vi.restoreAllMocks()
    // jsdom refuses real navigation ("Not implemented: navigation to another Document") —
    // login/register/logout intentionally trigger a full-page redirect, so replace `location`
    // with a plain writable object to observe where they tried to send the browser.
    // @ts-expect-error test-only override of a read-only global
    delete window.location
    // @ts-expect-error test-only override of a read-only global
    window.location = { href: '', pathname: '/profil', search: '' }
  })

  it('starts loading, then has no user when there is no active session', async () => {
    vi.mocked(authApi.getUserClaims).mockResolvedValue(null)
    const { result } = renderHook(() => useAuth(), { wrapper })

    expect(result.current.isLoading).toBe(true)

    await waitFor(() => expect(result.current.isLoading).toBe(false))
    expect(result.current.user).toBeNull()
  })

  it('derives the user from the session claims', async () => {
    vi.mocked(authApi.getUserClaims).mockResolvedValue(claims)
    const { result } = renderHook(() => useAuth(), { wrapper })

    await waitFor(() => expect(result.current.user?.email).toBe('demo@collector.shop'))
    expect(result.current.user).toEqual({
      userId: 'user-1',
      email: 'demo@collector.shop',
      displayName: 'Demo',
      isAdmin: false,
    })
  })

  it('marks the user as admin when the role claim is present', async () => {
    vi.mocked(authApi.getUserClaims).mockResolvedValue(adminClaims)
    const { result } = renderHook(() => useAuth(), { wrapper })

    await waitFor(() => expect(result.current.user?.isAdmin).toBe(true))
  })

  it('treats a session fetch failure as logged out', async () => {
    vi.mocked(authApi.getUserClaims).mockRejectedValue(new Error('network error'))
    const { result } = renderHook(() => useAuth(), { wrapper })

    await waitFor(() => expect(result.current.isLoading).toBe(false))
    expect(result.current.user).toBeNull()
  })

  it('login redirects to /bff/login', async () => {
    vi.mocked(authApi.getUserClaims).mockResolvedValue(null)
    const { result } = renderHook(() => useAuth(), { wrapper })
    await waitFor(() => expect(result.current.isLoading).toBe(false))

    act(() => result.current.login())

    expect(window.location.href).toContain('/bff/login?returnUrl=')
  })

  it('register also redirects to /bff/login (form selection happens on the identity page)', async () => {
    vi.mocked(authApi.getUserClaims).mockResolvedValue(null)
    const { result } = renderHook(() => useAuth(), { wrapper })
    await waitFor(() => expect(result.current.isLoading).toBe(false))

    act(() => result.current.register())

    expect(window.location.href).toContain('/bff/login?returnUrl=')
  })

  it('logout redirects to the bff:logout_url claim', async () => {
    vi.mocked(authApi.getUserClaims).mockResolvedValue(claims)
    const { result } = renderHook(() => useAuth(), { wrapper })
    await waitFor(() => expect(result.current.user).not.toBeNull())

    act(() => result.current.logout())

    expect(window.location.href).toBe('/bff/logout?sid=abc')
  })

  it('updateDisplayName updates the in-memory user only', async () => {
    vi.mocked(authApi.getUserClaims).mockResolvedValue(claims)
    const { result } = renderHook(() => useAuth(), { wrapper })
    await waitFor(() => expect(result.current.user).not.toBeNull())

    act(() => result.current.updateDisplayName('Nouveau pseudo'))

    expect(result.current.user?.displayName).toBe('Nouveau pseudo')
  })

  it('updateDisplayName does nothing when there is no user', async () => {
    vi.mocked(authApi.getUserClaims).mockResolvedValue(null)
    const { result } = renderHook(() => useAuth(), { wrapper })
    await waitFor(() => expect(result.current.isLoading).toBe(false))

    act(() => result.current.updateDisplayName('Nouveau pseudo'))

    expect(result.current.user).toBeNull()
  })

  it('throws when useAuth is used outside of AuthProvider', () => {
    expect(() =>
      renderHook(() => useAuth(), {
        wrapper: ({ children }) => (
          <QueryClientProvider client={createTestQueryClient()}>{children}</QueryClientProvider>
        ),
      }),
    ).toThrow('useAuth must be used within an AuthProvider')
  })
})
