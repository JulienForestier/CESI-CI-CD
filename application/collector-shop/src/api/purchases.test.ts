import { describe, expect, it, vi } from 'vitest'
import { getMyPurchases, purchaseListing } from './purchases'
import * as client from './client'

vi.mock('./client')

describe('purchases api', () => {
  it('purchaseListing POSTs /listings/:id/purchase', async () => {
    vi.mocked(client.apiFetch).mockResolvedValue(undefined)

    await purchaseListing('listing-1')

    expect(client.apiFetch).toHaveBeenCalledWith('/listings/listing-1/purchase', {
      method: 'POST',
    })
  })

  it('getMyPurchases calls /purchases/mine', async () => {
    vi.mocked(client.apiFetch).mockResolvedValue([])

    await getMyPurchases()

    expect(client.apiFetch).toHaveBeenCalledWith('/purchases/mine')
  })
})
