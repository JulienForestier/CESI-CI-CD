import { describe, expect, it, vi } from 'vitest'
import { getReports, reportListing } from './reports'
import * as client from './client'

vi.mock('./client')

describe('reports api', () => {
  it('reportListing POSTs /listings/:id/report with the reason and details', async () => {
    vi.mocked(client.apiFetch).mockResolvedValue({})

    await reportListing('listing-1', 'Contenu suspect', 'Détail')

    expect(client.apiFetch).toHaveBeenCalledWith('/listings/listing-1/report', {
      method: 'POST',
      body: { reason: 'Contenu suspect', details: 'Détail' },
    })
  })

  it('getReports calls /admin/reports without a query when no search term', async () => {
    vi.mocked(client.apiFetch).mockResolvedValue([])

    await getReports()

    expect(client.apiFetch).toHaveBeenCalledWith('/admin/reports')
  })

  it('getReports appends the encoded search term', async () => {
    vi.mocked(client.apiFetch).mockResolvedValue([])

    await getReports('paiement hors plateforme')

    expect(client.apiFetch).toHaveBeenCalledWith('/admin/reports?search=paiement%20hors%20plateforme')
  })
})
