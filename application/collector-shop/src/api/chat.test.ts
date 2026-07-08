import { describe, expect, it, vi } from 'vitest'
import { getConversations, getMessages, sendMessage, startConversation } from './chat'
import * as client from './client'

vi.mock('./client')

describe('chat api', () => {
  it('getConversations calls /conversations', async () => {
    vi.mocked(client.apiFetch).mockResolvedValue([])

    await getConversations()

    expect(client.apiFetch).toHaveBeenCalledWith('/conversations')
  })

  it('startConversation POSTs /conversations with the listingId', async () => {
    vi.mocked(client.apiFetch).mockResolvedValue({ id: 'conv-1' })

    await startConversation('listing-1')

    expect(client.apiFetch).toHaveBeenCalledWith('/conversations', {
      method: 'POST',
      body: { listingId: 'listing-1' },
    })
  })

  it('getMessages calls /conversations/:id/messages', async () => {
    vi.mocked(client.apiFetch).mockResolvedValue([])

    await getMessages('conv-1')

    expect(client.apiFetch).toHaveBeenCalledWith('/conversations/conv-1/messages')
  })

  it('sendMessage POSTs /conversations/:id/messages with the body', async () => {
    vi.mocked(client.apiFetch).mockResolvedValue({})

    await sendMessage('conv-1', 'Bonjour')

    expect(client.apiFetch).toHaveBeenCalledWith('/conversations/conv-1/messages', {
      method: 'POST',
      body: { body: 'Bonjour' },
    })
  })
})
