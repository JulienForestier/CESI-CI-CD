import { apiFetch } from './client'
import type { Conversation, Message } from '../types'

export function getConversations(token: string) {
  return apiFetch<Conversation[]>('/conversations', { token })
}

export function startConversation(token: string, listingId: string) {
  return apiFetch<{ id: string }>('/conversations', { method: 'POST', token, body: { listingId } })
}

export function getMessages(token: string, conversationId: string) {
  return apiFetch<Message[]>(`/conversations/${conversationId}/messages`, { token })
}

export function sendMessage(token: string, conversationId: string, body: string) {
  return apiFetch<Message>(`/conversations/${conversationId}/messages`, {
    method: 'POST',
    token,
    body: { body },
  })
}
