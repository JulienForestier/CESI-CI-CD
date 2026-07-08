import { describe, expect, it, vi } from 'vitest'
import { getConversations, getMessages, sendMessage, startConversation } from './chat'
import * as client from './client'

vi.mock('./client')

describe('chat api', () => {
  it('getConversations calls /conversations with the bearer token', async () => {
    vi.mocked(client.apiFetch).mockResolvedValue([])

    await getConversations('jwt-token')

    expect(client.apiFetch).toHaveBeenCalledWith('/conversations', { token: 'jwt-token' })
  })

  it('startConversation POSTs /conversations with the listingId', async () => {
    vi.mocked(client.apiFetch).mockResolvedValue({ id: 'conv-1' })

    await startConversation('jwt-token', 'listing-1')

    expect(client.apiFetch).toHaveBeenCalledWith('/conversations', {
      method: 'POST',
      token: 'jwt-token',
      body: { listingId: 'listing-1' },
    })
  })

  it('getMessages calls /conversations/:id/messages with the bearer token', async () => {
    vi.mocked(client.apiFetch).mockResolvedValue([])

    await getMessages('jwt-token', 'conv-1')

    expect(client.apiFetch).toHaveBeenCalledWith('/conversations/conv-1/messages', { token: 'jwt-token' })
  })

  it('sendMessage POSTs /conversations/:id/messages with the body', async () => {
    vi.mocked(client.apiFetch).mockResolvedValue({})

    await sendMessage('jwt-token', 'conv-1', 'Bonjour')

    expect(client.apiFetch).toHaveBeenCalledWith('/conversations/conv-1/messages', {
      method: 'POST',
      token: 'jwt-token',
      body: { body: 'Bonjour' },
    })
  })
})
