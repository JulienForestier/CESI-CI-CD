import { useState, type FormEvent } from 'react'
import { ApiError, register } from './api'
import { getReturnUrl } from './returnUrl'

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
      window.location.href = returnUrl
    } catch (err) {
      setError(
        err instanceof ApiError && err.status === 409
          ? 'Un compte existe déjà avec cet email.'
          : err instanceof ApiError && err.status === 400
            ? err.message
            : 'Une erreur est survenue, réessayez.',
      )
      setIsSubmitting(false)
    }
  }

  return (
    <div className="mx-auto mt-20 max-w-md rounded-2xl border-[1.5px] border-ink/15 bg-card p-10 text-center">
      <span className="mx-auto mb-5 flex h-11 w-11 items-center justify-center rounded-full border-2 border-ink font-display text-xl">
        C
      </span>
      <h1 className="font-display text-3xl">Créer un compte</h1>
      <p className="mt-1.5 mb-7 font-ui text-sm text-brown-2">Rejoignez la communauté des collectionneurs</p>

      <form onSubmit={onSubmit} className="flex flex-col gap-3.5 text-left font-ui">
        <label className="flex flex-col gap-1.5 text-xs font-semibold text-brown-2">
          Pseudo
          <input
            required
            value={displayName}
            onChange={(e) => setDisplayName(e.target.value)}
            className="rounded-lg border-[1.5px] border-ink bg-surface px-3.5 py-3 text-sm text-ink"
          />
        </label>
        <label className="flex flex-col gap-1.5 text-xs font-semibold text-brown-2">
          Adresse email
          <input
            type="email"
            required
            value={email}
            onChange={(e) => setEmail(e.target.value)}
            className="rounded-lg border-[1.5px] border-ink bg-surface px-3.5 py-3 text-sm text-ink"
          />
        </label>
        <label className="flex flex-col gap-1.5 text-xs font-semibold text-brown-2">
          Mot de passe
          <input
            type="password"
            required
            minLength={8}
            value={password}
            onChange={(e) => setPassword(e.target.value)}
            className="rounded-lg border-[1.5px] border-ink bg-surface px-3.5 py-3 text-sm text-ink"
          />
        </label>

        {error && <p className="text-xs font-medium text-burnt">{error}</p>}

        <button
          type="submit"
          disabled={isSubmitting}
          className="mt-1.5 rounded-xl bg-burnt py-3.5 font-ui text-[15px] font-bold text-surface shadow-[4px_4px_0_#29211b] transition disabled:opacity-50"
        >
          {isSubmitting ? 'Création…' : "S'inscrire"}
        </button>
      </form>

      <p className="mt-5 font-ui text-sm text-brown-2">
        Déjà un compte ?{' '}
        <a href={`/login${window.location.search}`} className="font-semibold text-burnt hover:underline">
          Se connecter
        </a>
      </p>
    </div>
  )
}
