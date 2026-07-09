import type { FormEvent, ReactNode } from 'react'

interface AuthFormShellProps {
  title: string
  subtitle: string
  onSubmit: (event: FormEvent) => void
  error: string | null
  isSubmitting: boolean
  submitLabel: string
  submittingLabel: string
  footerText: string
  footerLinkHref: string
  footerLinkLabel: string
  children: ReactNode
}

export function AuthFormShell({
  title,
  subtitle,
  onSubmit,
  error,
  isSubmitting,
  submitLabel,
  submittingLabel,
  footerText,
  footerLinkHref,
  footerLinkLabel,
  children,
}: Readonly<AuthFormShellProps>) {
  return (
    <div className="mx-auto max-w-md rounded-2xl border-[1.5px] border-ink/15 bg-card p-10 text-center">
      <span className="mx-auto mb-5 flex h-11 w-11 items-center justify-center rounded-full border-2 border-ink font-display text-xl">
        C
      </span>
      <h1 className="font-display text-3xl">{title}</h1>
      <p className="mt-1.5 mb-7 font-ui text-sm text-brown-2">{subtitle}</p>

      <form onSubmit={onSubmit} className="flex flex-col gap-3.5 text-left font-ui">
        {children}

        {error && <p className="text-xs font-medium text-burnt">{error}</p>}

        <button
          type="submit"
          disabled={isSubmitting}
          className="mt-1.5 rounded-xl bg-burnt py-3.5 font-ui text-[15px] font-bold text-surface shadow-[4px_4px_0_#29211b] transition disabled:opacity-50"
        >
          {isSubmitting ? submittingLabel : submitLabel}
        </button>
      </form>

      <p className="mt-5 font-ui text-sm text-brown-2">
        {footerText} <a href={footerLinkHref} className="font-semibold text-burnt hover:underline">{footerLinkLabel}</a>
      </p>
    </div>
  )
}
