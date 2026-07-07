import { createContext, useCallback, useContext, useMemo, useState, type ReactNode } from 'react'
import { useMutation } from '@tanstack/react-query'
import * as authApi from '../api/auth'
import type { AuthResponse } from '../types'

export interface AuthUser {
  token: string
  userId: string
  email: string
  displayName: string
  isAdmin: boolean
}

interface AuthContextValue {
  user: AuthUser | null
  login: (email: string, password: string) => Promise<void>
  register: (email: string, password: string, displayName: string) => Promise<void>
  logout: () => void
}

const STORAGE_KEY = 'collector-shop-auth'

const AuthContext = createContext<AuthContextValue | undefined>(undefined)

function readStoredUser(): AuthUser | null {
  const raw = localStorage.getItem(STORAGE_KEY)
  if (!raw) return null
  try {
    return JSON.parse(raw) as AuthUser
  } catch {
    return null
  }
}

function toAuthUser(response: AuthResponse): AuthUser {
  return {
    token: response.token,
    userId: response.userId,
    email: response.email,
    displayName: response.displayName,
    isAdmin: response.isAdmin,
  }
}

export function AuthProvider({ children }: { children: ReactNode }) {
  const [user, setUser] = useState<AuthUser | null>(() => readStoredUser())

  const persist = useCallback((next: AuthUser | null) => {
    setUser(next)
    if (next) {
      localStorage.setItem(STORAGE_KEY, JSON.stringify(next))
    } else {
      localStorage.removeItem(STORAGE_KEY)
    }
  }, [])

  const loginMutation = useMutation({
    mutationFn: ({ email, password }: { email: string; password: string }) => authApi.login(email, password),
    onSuccess: (response) => persist(toAuthUser(response)),
  })

  const registerMutation = useMutation({
    mutationFn: ({ email, password, displayName }: { email: string; password: string; displayName: string }) =>
      authApi.register(email, password, displayName),
    onSuccess: (response) => persist(toAuthUser(response)),
  })

  const login = useCallback(
    (email: string, password: string) => loginMutation.mutateAsync({ email, password }).then(() => undefined),
    [loginMutation],
  )

  const register = useCallback(
    (email: string, password: string, displayName: string) =>
      registerMutation.mutateAsync({ email, password, displayName }).then(() => undefined),
    [registerMutation],
  )

  const logout = useCallback(() => persist(null), [persist])

  const value = useMemo(() => ({ user, login, register, logout }), [user, login, register, logout])

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>
}

export function useAuth(): AuthContextValue {
  const ctx = useContext(AuthContext)
  if (!ctx) {
    throw new Error('useAuth must be used within an AuthProvider')
  }
  return ctx
}
