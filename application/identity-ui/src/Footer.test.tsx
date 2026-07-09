import { render, screen } from '@testing-library/react'
import { describe, expect, it } from 'vitest'
import { Footer } from './Footer'

describe('Footer', () => {
  it('renders the three reassurance columns', () => {
    render(<Footer />)

    expect(screen.getByText('Paiement sécurisé')).toBeInTheDocument()
    expect(screen.getByText('Contrôle qualité')).toBeInTheDocument()
    expect(screen.getByText('Chat intégré')).toBeInTheDocument()
    expect(
      screen.getByText("Toutes les transactions passent par Collector. Zéro échange d'argent en direct."),
    ).toBeInTheDocument()
  })
})
