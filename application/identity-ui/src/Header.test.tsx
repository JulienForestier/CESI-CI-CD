import { render, screen } from '@testing-library/react'
import { describe, expect, it } from 'vitest'
import { Header } from './Header'

describe('Header', () => {
  it('renders the Collector.shop logo linking back to the main app', () => {
    render(<Header />)

    const logoLink = screen.getByRole('link', { name: /Collector\.shop/ })
    expect(logoLink).toHaveAttribute('href', '/')
  })
})
