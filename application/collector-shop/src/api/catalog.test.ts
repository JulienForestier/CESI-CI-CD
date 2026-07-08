import { describe, expect, it, vi } from 'vitest'
import * as client from './client'
import { createListing, getCategories, getListing, getListings, getMyListings } from './catalog'

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

    await getListings({ categoryId: 'cat-1' })

    expect(client.apiFetch).toHaveBeenCalledWith('/listings?categoryId=cat-1')
  })

  it('getListings appends the search term when provided', async () => {
    vi.mocked(client.apiFetch).mockResolvedValue([])

    await getListings({ search: 'goku' })

    expect(client.apiFetch).toHaveBeenCalledWith('/listings?search=goku')
  })

  it('getListings combines category and search filters', async () => {
    vi.mocked(client.apiFetch).mockResolvedValue([])

    await getListings({ categoryId: 'cat-1', search: 'goku' })

    expect(client.apiFetch).toHaveBeenCalledWith('/listings?categoryId=cat-1&search=goku')
  })

  it('getListing calls /listings/:id', async () => {
    vi.mocked(client.apiFetch).mockResolvedValue({})

    await getListing('listing-1')

    expect(client.apiFetch).toHaveBeenCalledWith('/listings/listing-1')
  })

  it('getMyListings calls /listings/mine', async () => {
    vi.mocked(client.apiFetch).mockResolvedValue([])

    await getMyListings()

    expect(client.apiFetch).toHaveBeenCalledWith('/listings/mine')
  })

  it('createListing posts', async () => {
    vi.mocked(client.apiFetch).mockResolvedValue({})
    const input = { title: 'T', description: 'D', price: 10, categoryId: 'cat-1' }

    await createListing(input)

    expect(client.apiFetch).toHaveBeenCalledWith('/listings', {
      method: 'POST',
      body: input,
    })
  })
})
