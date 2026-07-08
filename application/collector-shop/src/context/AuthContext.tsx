import { createContext, useCallback, useContext, useEffect, useMemo, useState, type ReactNode } from 'react'
import * as authApi from '../api/auth'
import type { BffClaim } from '../api/auth'

export interface AuthUser {
  userId: string
  email: string
  displayName: string
  isAdmin: boolean
}

interface AuthContextValue {
  user: AuthUser | null
  isLoading: boolean
  login: () => void
  register: () => void
  logout: () => void
  updateDisplayName: (displayName: string) => void
}

const AuthContext = createContext<AuthContextValue | undefined>(undefined)

function findClaim(claims: BffClaim[], type: string): string | undefined {
  return claims.find((c) => c.type === type)?.value
}

function toAuthUser(claims: BffClaim[]): AuthUser | null {
  const userId = findClaim(claims, 'sub')
  const email = findClaim(claims, 'email')
  const displayName = findClaim(claims, 'name')
  if (!userId || !email || !displayName) return null

  return {
    userId,
    email,
    displayName,
    isAdmin: findClaim(claims, 'role') === 'Admin',
  }
}

export function AuthProvider({ children }: Readonly<{ children: ReactNode }>) {
  const [user, setUser] = useState<AuthUser | null>(null)
  const [logoutUrl, setLogoutUrl] = useState<string | null>(null)
  const [isLoading, setIsLoading] = useState(true)

  useEffect(() => {
    let cancelled = false

    authApi
      .getUserClaims()
      .then((claims) => {
        if (cancelled) return
        if (!claims) {
          setUser(null)
          setLogoutUrl(null)
          return
        }
        setUser(toAuthUser(claims))
        setLogoutUrl(findClaim(claims, 'bff:logout_url') ?? null)
      })
      .catch(() => {
        if (!cancelled) setUser(null)
      })
      .finally(() => {
        if (!cancelled) setIsLoading(false)
      })

    return () => {
      cancelled = true
    }
  }, [])

  // Redirection plein-écran obligatoire (pattern Duende.BFF) — pas de fetch/POST possible ici,
  // /bff/login déclenche le flow OIDC Authorization Code + PKCE vers l'IdentityService.
  const login = useCallback(() => {
    window.location.href = `/bff/login?returnUrl=${encodeURIComponent(window.location.pathname)}`
  }, [])

  // Même redirection : le choix "connexion" / "création de compte" se fait sur la page de
  // l'IdentityService, pas ici (Duende.BFF n'expose qu'un seul point d'entrée /bff/login).
  const register = login

  const logout = useCallback(() => {
    if (logoutUrl) {
      window.location.href = logoutUrl
    }
  }, [logoutUrl])

  const updateDisplayName = useCallback((displayName: string) => {
    setUser((current) => (current ? { ...current, displayName } : current))
  }, [])

  const value = useMemo(
    () => ({ user, isLoading, login, register, logout, updateDisplayName }),
    [user, isLoading, login, register, logout, updateDisplayName],
  )

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>
}

export function useAuth(): AuthContextValue {
  const ctx = useContext(AuthContext)
  if (!ctx) {
    throw new Error('useAuth must be used within an AuthProvider')
  }
  return ctx
}
