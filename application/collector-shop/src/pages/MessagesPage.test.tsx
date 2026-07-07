import { render, screen, waitFor } from '@testing-library/react'
import { QueryClientProvider } from '@tanstack/react-query'
import userEvent from '@testing-library/user-event'
import { MemoryRouter, Route, Routes } from 'react-router-dom'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import { MessagesPage } from './MessagesPage'
import { ApiError } from '../api/client'
import * as chatApi from '../api/chat'
import * as AuthContext from '../context/AuthContext'
import { createTestQueryClient } from '../test/queryClient'
import type { Conversation, Message } from '../types'

vi.mock('../api/chat')
vi.mock('../context/AuthContext', async () => {
  const actual = await vi.importActual<typeof AuthContext>('../context/AuthContext')
  return { ...actual, useAuth: vi.fn() }
})

const conversation: Conversation = {
  id: 'conv-1',
  listingId: 'listing-1',
  listingTitle: 'Figurine Goku',
  counterpartId: 'seller-1',
  counterpartDisplayName: 'Vendeur Test',
  lastMessageBody: 'Bonjour, toujours dispo ?',
  lastMessageAt: new Date().toISOString(),
  hasUnread: true,
}

const message: Message = {
  id: 'msg-1',
  conversationId: 'conv-1',
  senderId: 'buyer-1',
  senderDisplayName: 'Acheteur Test',
  body: 'Bonjour, toujours dispo ?',
  sentAt: new Date().toISOString(),
}

function renderAt(path: string) {
  return render(
    <QueryClientProvider client={createTestQueryClient()}>
      <MemoryRouter initialEntries={[path]}>
        <Routes>
          <Route path="/messages" element={<MessagesPage />} />
          <Route path="/messages/:conversationId" element={<MessagesPage />} />
        </Routes>
      </MemoryRouter>
    </QueryClientProvider>,
  )
}

describe('MessagesPage', () => {
  beforeEach(() => {
    vi.restoreAllMocks()
    vi.mocked(AuthContext.useAuth).mockReturnValue({
      user: { token: 'jwt-token', userId: 'buyer-1', email: 'a@b.com', displayName: 'Acheteur Test', isAdmin: false },
      login: vi.fn(),
      register: vi.fn(),
      logout: vi.fn(),
    })
  })

  it('shows an empty state when there are no conversations', async () => {
    vi.mocked(chatApi.getConversations).mockResolvedValue([])

    renderAt('/messages')

    expect(await screen.findByText('Aucune conversation pour le moment.')).toBeInTheDocument()
    expect(screen.getByText('Sélectionnez une conversation pour afficher les messages.')).toBeInTheDocument()
  })

  it('shows the conversation list with an unread indicator', async () => {
    vi.mocked(chatApi.getConversations).mockResolvedValue([conversation])

    renderAt('/messages')

    expect(await screen.findByText('Vendeur Test')).toBeInTheDocument()
    expect(screen.getByText('Figurine Goku')).toBeInTheDocument()
    expect(screen.getByLabelText('Non lu')).toBeInTheDocument()
  })

  it('loads and displays messages for the selected conversation', async () => {
    vi.mocked(chatApi.getConversations).mockResolvedValue([conversation])
    vi.mocked(chatApi.getMessages).mockResolvedValue([message])

    renderAt('/messages/conv-1')

    expect(await screen.findByText('Bonjour, toujours dispo ?')).toBeInTheDocument()
    expect(chatApi.getMessages).toHaveBeenCalledWith('jwt-token', 'conv-1')
  })

  it('sends a new message and clears the input', async () => {
    vi.mocked(chatApi.getConversations).mockResolvedValue([conversation])
    vi.mocked(chatApi.getMessages).mockResolvedValue([])
    vi.mocked(chatApi.sendMessage).mockResolvedValue({ ...message, id: 'msg-2', body: 'Salut !' })

    renderAt('/messages/conv-1')

    const input = await screen.findByPlaceholderText('Écrivez votre message…')
    await userEvent.type(input, 'Salut !')
    await userEvent.click(screen.getByRole('button', { name: 'Envoyer' }))

    await waitFor(() => expect(chatApi.sendMessage).toHaveBeenCalledWith('jwt-token', 'conv-1', 'Salut !'))
    await waitFor(() => expect(input).toHaveValue(''))
  })

  it('shows a server error when the message is rejected for containing contact info', async () => {
    vi.mocked(chatApi.getConversations).mockResolvedValue([conversation])
    vi.mocked(chatApi.getMessages).mockResolvedValue([])
    vi.mocked(chatApi.sendMessage).mockRejectedValue(
      new ApiError(400, "Le partage de coordonnées personnelles (email, téléphone) n'est pas autorisé sur Collector.shop."),
    )

    renderAt('/messages/conv-1')

    const input = await screen.findByPlaceholderText('Écrivez votre message…')
    await userEvent.type(input, 'appelle moi au 0612345678')
    await userEvent.click(screen.getByRole('button', { name: 'Envoyer' }))

    expect(
      await screen.findByText("Le partage de coordonnées personnelles (email, téléphone) n'est pas autorisé sur Collector.shop."),
    ).toBeInTheDocument()
  })
})
