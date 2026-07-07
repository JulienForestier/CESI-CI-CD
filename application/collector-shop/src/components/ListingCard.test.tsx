import { render, screen } from '@testing-library/react'
import { MemoryRouter } from 'react-router-dom'
import { describe, expect, it } from 'vitest'
import { ListingCard } from './ListingCard'
import type { Listing } from '../types'

const listing: Listing = {
  id: 'listing-1',
  title: 'Figurine Goku',
  description: 'Édition limitée',
  price: 45.9,
  status: 'Published',
  createdAt: new Date().toISOString(),
  sellerId: 'seller-1',
  sellerDisplayName: 'Vendeur Test',
  categoryId: 'cat-1',
  categoryName: 'Figurines',
}

describe('ListingCard', () => {
  it('renders listing details and links to the detail page', () => {
    render(
      <MemoryRouter>
        <ListingCard listing={listing} />
      </MemoryRouter>,
    )

    expect(screen.getByText('Figurine Goku')).toBeInTheDocument()
    expect(screen.getByText('Figurines')).toBeInTheDocument()
    expect(screen.getByText('45,90 €')).toBeInTheDocument()
    expect(screen.getByText('Vendeur Test')).toBeInTheDocument()
    expect(screen.getByRole('link')).toHaveAttribute('href', '/annonces/listing-1')
  })
})
