export interface BffClaim {
  type: string
  value: string
}

/**
 * Session gérée par Duende.BFF (cookie HttpOnly côté ApiService) — pas de login/register ici,
 * ils vivent sur l'IdentityService (redirection plein-écran, voir AuthContext.login/register).
 */
export async function getUserClaims(): Promise<BffClaim[] | null> {
  const response = await fetch('/bff/user', {
    credentials: 'include',
    headers: { 'X-CSRF': '1' },
  })

  if (response.status === 401) {
    return null
  }
  if (!response.ok) {
    throw new Error("Impossible de récupérer l'état de la session.")
  }

  const claims = (await response.json()) as BffClaim[]
  return claims.length > 0 ? claims : null
}
