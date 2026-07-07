import { describe, expect, it, vi } from 'vitest'
import { approveListing, getPendingListings, rejectListing } from './moderation'
import * as client from './client'

vi.mock('./client')

describe('moderation api', () => {
  it('getPendingListings calls /admin/listings/pending with the bearer token', async () => {
    vi.mocked(client.apiFetch).mockResolvedValue([])

    await getPendingListings('jwt-token')

    expect(client.apiFetch).toHaveBeenCalledWith('/admin/listings/pending', { token: 'jwt-token' })
  })

  it('approveListing POSTs /admin/listings/:id/approve', async () => {
    vi.mocked(client.apiFetch).mockResolvedValue({})

    await approveListing('jwt-token', 'listing-1')

    expect(client.apiFetch).toHaveBeenCalledWith('/admin/listings/listing-1/approve', {
      method: 'POST',
      token: 'jwt-token',
    })
  })

  it('rejectListing POSTs /admin/listings/:id/reject with the reason', async () => {
    vi.mocked(client.apiFetch).mockResolvedValue({})

    await rejectListing('jwt-token', 'listing-1', 'Titre non conforme')

    expect(client.apiFetch).toHaveBeenCalledWith('/admin/listings/listing-1/reject', {
      method: 'POST',
      token: 'jwt-token',
      body: { reason: 'Titre non conforme' },
    })
  })
})
