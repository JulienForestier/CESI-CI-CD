import { act, renderHook, waitFor } from '@testing-library/react'
import { QueryClientProvider } from '@tanstack/react-query'
import type { ReactNode } from 'react'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import * as authApi from '../api/auth'
import { createTestQueryClient } from '../test/queryClient'
import { AuthProvider, useAuth } from './AuthContext'

vi.mock('../api/auth')

const authResponse = {
  token: 'jwt-token',
  userId: 'user-1',
  email: 'demo@collector.shop',
  displayName: 'Demo',
  isAdmin: false,
}

function wrapper({ children }: { children: ReactNode }) {
  return (
    <QueryClientProvider client={createTestQueryClient()}>
      <AuthProvider>{children}</AuthProvider>
    </QueryClientProvider>
  )
}

describe('AuthContext', () => {
  beforeEach(() => {
    localStorage.clear()
    vi.restoreAllMocks()
  })

  it('starts with no user when nothing is stored', () => {
    const { result } = renderHook(() => useAuth(), { wrapper })

    expect(result.current.user).toBeNull()
  })

  it('restores the user from localStorage on init', () => {
    localStorage.setItem('collector-shop-auth', JSON.stringify(authResponse))

    const { result } = renderHook(() => useAuth(), { wrapper })

    expect(result.current.user?.email).toBe('demo@collector.shop')
  })

  it('ignores corrupted localStorage content', () => {
    localStorage.setItem('collector-shop-auth', 'not-json')

    const { result } = renderHook(() => useAuth(), { wrapper })

    expect(result.current.user).toBeNull()
  })

  it('login stores the user and persists it', async () => {
    vi.mocked(authApi.login).mockResolvedValue(authResponse)
    const { result } = renderHook(() => useAuth(), { wrapper })

    await act(async () => {
      await result.current.login('demo@collector.shop', 'password')
    })

    await waitFor(() => expect(result.current.user?.token).toBe('jwt-token'))
    expect(JSON.parse(localStorage.getItem('collector-shop-auth')!).userId).toBe('user-1')
  })

  it('register stores the user', async () => {
    vi.mocked(authApi.register).mockResolvedValue(authResponse)
    const { result } = renderHook(() => useAuth(), { wrapper })

    await act(async () => {
      await result.current.register('demo@collector.shop', 'password', 'Demo')
    })

    expect(result.current.user?.displayName).toBe('Demo')
  })

  it('logout clears the user and localStorage', async () => {
    vi.mocked(authApi.login).mockResolvedValue(authResponse)
    const { result } = renderHook(() => useAuth(), { wrapper })

    await act(async () => {
      await result.current.login('demo@collector.shop', 'password')
    })
    act(() => result.current.logout())

    expect(result.current.user).toBeNull()
    expect(localStorage.getItem('collector-shop-auth')).toBeNull()
  })

  it('updateDisplayName updates the user and persists it', async () => {
    vi.mocked(authApi.login).mockResolvedValue(authResponse)
    const { result } = renderHook(() => useAuth(), { wrapper })

    await act(async () => {
      await result.current.login('demo@collector.shop', 'password')
    })
    act(() => result.current.updateDisplayName('Nouveau pseudo'))

    expect(result.current.user?.displayName).toBe('Nouveau pseudo')
    expect(JSON.parse(localStorage.getItem('collector-shop-auth')!).displayName).toBe('Nouveau pseudo')
  })

  it('updateDisplayName does nothing when there is no user', () => {
    const { result } = renderHook(() => useAuth(), { wrapper })

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
