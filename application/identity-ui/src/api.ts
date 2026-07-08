export class ApiError extends Error {
  status: number

  constructor(status: number, message: string) {
    super(message)
    this.name = 'ApiError'
    this.status = status
  }
}

async function post(path: string, body: unknown): Promise<{ returnUrl: string }> {
  const response = await fetch(path, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    credentials: 'include',
    body: JSON.stringify(body),
  })

  if (!response.ok) {
    const errorBody = (await response.json().catch(() => null)) as { message?: string } | null
    throw new ApiError(response.status, errorBody?.message ?? response.statusText)
  }

  return (await response.json()) as { returnUrl: string }
}

export function login(email: string, password: string, returnUrl: string | null) {
  return post('/account/login', { email, password, returnUrl })
}

export function register(email: string, password: string, displayName: string, returnUrl: string | null) {
  return post('/account/register', { email, password, displayName, returnUrl })
}
