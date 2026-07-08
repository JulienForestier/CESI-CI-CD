import { useState } from 'react'
import { zodResolver } from '@hookform/resolvers/zod'
import { useForm } from 'react-hook-form'
import { Link, useNavigate } from 'react-router-dom'
import { ApiError } from '../api/client'
import { useAuth } from '../context/AuthContext'
import { registerSchema, type RegisterFormValues } from '../schemas/auth'

export function RegisterPage() {
  const { register: registerUser } = useAuth()
  const navigate = useNavigate()
  const [error, setError] = useState<string | null>(null)
  const {
    register,
    handleSubmit,
    formState: { errors, isSubmitting },
  } = useForm<RegisterFormValues>({ resolver: zodResolver(registerSchema) })

  async function onSubmit(values: RegisterFormValues) {
    setError(null)
    try {
      await registerUser(values.email, values.password, values.displayName)
      navigate('/')
    } catch (err) {
      setError(
        err instanceof ApiError && err.status === 409
          ? 'Un compte existe déjà avec cet email.'
          : 'Une erreur est survenue, réessayez.',
      )
    }
  }

  return (
    <div className="mx-auto max-w-md rounded-2xl border-[1.5px] border-ink/15 bg-card p-10 text-center">
      <span className="mx-auto mb-5 flex h-11 w-11 items-center justify-center rounded-full border-2 border-ink font-display text-xl">
        C
      </span>
      <h1 className="font-display text-3xl">Créer un compte</h1>
      <p className="mt-1.5 mb-7 font-ui text-sm text-brown-2">
        Rejoignez la communauté des collectionneurs
      </p>

      <form onSubmit={handleSubmit(onSubmit)} className="flex flex-col gap-3.5 text-left font-ui">
        <label className="flex flex-col gap-1.5 text-xs font-semibold text-brown-2">
          Pseudo
          <input
            {...register('displayName')}
            className="rounded-lg border-[1.5px] border-ink bg-surface px-3.5 py-3 text-sm text-ink"
          />
          {errors.displayName && (
            <span className="text-xs font-medium text-burnt">{errors.displayName.message}</span>
          )}
        </label>
        <label className="flex flex-col gap-1.5 text-xs font-semibold text-brown-2">
          Adresse email
          <input
            type="email"
            {...register('email')}
            className="rounded-lg border-[1.5px] border-ink bg-surface px-3.5 py-3 text-sm text-ink"
          />
          {errors.email && <span className="text-xs font-medium text-burnt">{errors.email.message}</span>}
        </label>
        <label className="flex flex-col gap-1.5 text-xs font-semibold text-brown-2">
          Mot de passe
          <input
            type="password"
            {...register('password')}
            className="rounded-lg border-[1.5px] border-ink bg-surface px-3.5 py-3 text-sm text-ink"
          />
          {errors.password && <span className="text-xs font-medium text-burnt">{errors.password.message}</span>}
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
        <Link to="/connexion" className="font-semibold text-burnt hover:underline">
          Se connecter
        </Link>
      </p>
    </div>
  )
}
