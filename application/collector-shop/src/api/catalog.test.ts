import { describe, expect, it, vi } from 'vitest'
import * as client from './client'
import { createListing, getCategories, getListing, getListings } from './catalog'

vi.mock('./client')

describe('catalog api', () => {
  it('getCategories calls /categories', async () => {
    vi.mocked(client.apiFetch).mockResolvedValue([])

    await getCategories()

    expect(client.apiFetch).toHaveBeenCalledWith('/categories')
  })

  it('getListings calls /listings without a filter', async () => {
    vi.mocked(client.apiFetch).mockResolvedValue([])

    await getListings()

    expect(client.apiFetch).toHaveBeenCalledWith('/listings')
  })

  it('getListings appends the category filter when provided', async () => {
    vi.mocked(client.apiFetch).mockResolvedValue([])

    await getListings('cat-1')

    expect(client.apiFetch).toHaveBeenCalledWith('/listings?categoryId=cat-1')
  })

  it('getListing calls /listings/:id', async () => {
    vi.mocked(client.apiFetch).mockResolvedValue({})

    await getListing('listing-1')

    expect(client.apiFetch).toHaveBeenCalledWith('/listings/listing-1')
  })

  it('createListing posts with the bearer token', async () => {
    vi.mocked(client.apiFetch).mockResolvedValue({})
    const input = { title: 'T', description: 'D', price: 10, categoryId: 'cat-1' }

    await createListing('jwt-token', input)

    expect(client.apiFetch).toHaveBeenCalledWith('/listings', {
      method: 'POST',
      body: input,
      token: 'jwt-token',
    })
  })
})
