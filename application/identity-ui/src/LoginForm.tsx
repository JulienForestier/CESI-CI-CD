import { useState, type FormEvent } from 'react'
import { AuthFormShell } from './AuthFormShell'
import { ApiError, login } from './api'
import { getReturnUrl, navigateToReturnUrl } from './returnUrl'

export function LoginForm() {
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [error, setError] = useState<string | null>(null)
  const [isSubmitting, setIsSubmitting] = useState(false)

  async function onSubmit(event: FormEvent) {
    event.preventDefault()
    setError(null)
    setIsSubmitting(true)
    try {
      const { returnUrl } = await login(email, password, getReturnUrl())
      navigateToReturnUrl(returnUrl)
    } catch (err) {
      setError(
        err instanceof ApiError && err.status === 401
          ? 'Email ou mot de passe incorrect.'
          : 'Une erreur est survenue, réessayez.',
      )
      setIsSubmitting(false)
    }
  }

  return (
    <AuthFormShell
      title="Bon retour parmi nous"
      subtitle="Connectez-vous pour retrouver votre espace"
      onSubmit={onSubmit}
      error={error}
      isSubmitting={isSubmitting}
      submitLabel="Se connecter"
      submittingLabel="Connexion…"
      footerText="Pas encore de compte ?"
      footerLinkHref={`/register${window.location.search}`}
      footerLinkLabel="Créer un compte"
    >
      <label className="flex flex-col gap-1.5 text-xs font-semibold text-brown-2">
        <span>Adresse email</span>
        <input
          type="email"
          required
          value={email}
          onChange={(e) => setEmail(e.target.value)}
          className="rounded-lg border-[1.5px] border-ink bg-surface px-3.5 py-3 text-sm text-ink"
        />
      </label>
      <label className="flex flex-col gap-1.5 text-xs font-semibold text-brown-2">
        <span>Mot de passe</span>
        <input
          type="password"
          required
          minLength={8}
          value={password}
          onChange={(e) => setPassword(e.target.value)}
          className="rounded-lg border-[1.5px] border-ink bg-surface px-3.5 py-3 text-sm text-ink"
        />
      </label>
    </AuthFormShell>
  )
}
