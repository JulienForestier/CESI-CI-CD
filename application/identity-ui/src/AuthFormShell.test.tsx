import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { describe, expect, it, vi } from 'vitest'
import { AuthFormShell } from './AuthFormShell'

function renderShell(overrides: Partial<React.ComponentProps<typeof AuthFormShell>> = {}) {
  const onSubmit = vi.fn((event) => event.preventDefault())
  render(
    <AuthFormShell
      title="Titre"
      subtitle="Sous-titre"
      onSubmit={onSubmit}
      error={null}
      isSubmitting={false}
      submitLabel="Valider"
      submittingLabel="En cours…"
      footerText="Une question ?"
      footerLinkHref="/autre"
      footerLinkLabel="Par ici"
      {...overrides}
    >
      <input aria-label="champ" />
    </AuthFormShell>,
  )
  return { onSubmit }
}

describe('AuthFormShell', () => {
  it('renders title, subtitle, children and footer link', () => {
    renderShell()

    expect(screen.getByRole('heading', { name: 'Titre' })).toBeInTheDocument()
    expect(screen.getByText('Sous-titre')).toBeInTheDocument()
    expect(screen.getByLabelText('champ')).toBeInTheDocument()
    expect(screen.getByRole('link', { name: 'Par ici' })).toHaveAttribute('href', '/autre')
  })

  it('shows the submit label when not submitting, and calls onSubmit', async () => {
    const { onSubmit } = renderShell()

    const button = screen.getByRole('button', { name: 'Valider' })
    await userEvent.click(button)

    expect(onSubmit).toHaveBeenCalled()
  })

  it('shows the submitting label and disables the button while submitting', () => {
    renderShell({ isSubmitting: true })

    const button = screen.getByRole('button', { name: 'En cours…' })
    expect(button).toBeDisabled()
  })

  it('displays the error message when provided', () => {
    renderShell({ error: 'Une erreur est survenue.' })

    expect(screen.getByText('Une erreur est survenue.')).toBeInTheDocument()
  })

  it('does not render an error paragraph when there is no error', () => {
    renderShell({ error: null })

    expect(screen.queryByText(/erreur/i)).not.toBeInTheDocument()
  })
})
