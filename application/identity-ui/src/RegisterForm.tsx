import { useState, type FormEvent } from 'react'
import { AuthFormShell } from './AuthFormShell'
import { ApiError, register } from './api'
import { getReturnUrl, navigateToReturnUrl } from './returnUrl'

function getErrorMessage(err: unknown): string {
  if (!(err instanceof ApiError)) {
    return 'Une erreur est survenue, réessayez.'
  }
  if (err.status === 409) {
    return 'Un compte existe déjà avec cet email.'
  }
  if (err.status === 400) {
    return err.message
  }
  return 'Une erreur est survenue, réessayez.'
}

export function RegisterForm() {
  const [displayName, setDisplayName] = useState('')
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [error, setError] = useState<string | null>(null)
  const [isSubmitting, setIsSubmitting] = useState(false)

  async function onSubmit(event: FormEvent) {
    event.preventDefault()
    setError(null)
    setIsSubmitting(true)
    try {
      const { returnUrl } = await register(email, password, displayName, getReturnUrl())
      navigateToReturnUrl(returnUrl)
    } catch (err) {
      setError(getErrorMessage(err))
      setIsSubmitting(false)
    }
  }

  return (
    <AuthFormShell
      title="Créer un compte"
      subtitle="Rejoignez la communauté des collectionneurs"
      onSubmit={onSubmit}
      error={error}
      isSubmitting={isSubmitting}
      submitLabel="S'inscrire"
      submittingLabel="Création…"
      footerText="Déjà un compte ?"
      footerLinkHref={`/login${window.location.search}`}
      footerLinkLabel="Se connecter"
    >
      <label className="flex flex-col gap-1.5 text-xs font-semibold text-brown-2">
        <span>Pseudo</span>
        <input
          required
          value={displayName}
          onChange={(e) => setDisplayName(e.target.value)}
          className="rounded-lg border-[1.5px] border-ink bg-surface px-3.5 py-3 text-sm text-ink"
        />
      </label>
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
