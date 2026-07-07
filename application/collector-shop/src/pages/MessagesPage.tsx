import { useState, type FormEvent } from 'react'
import { Link, useParams } from 'react-router-dom'
import { ApiError } from '../api/client'
import { useAuth } from '../context/AuthContext'
import { useConversations, useMessages, useSendMessage } from '../hooks/useChat'
import type { Conversation } from '../types'

export function MessagesPage() {
  const { conversationId } = useParams<{ conversationId: string }>()
  const conversationsQuery = useConversations()
  const conversations = conversationsQuery.data ?? []
  const activeConversation = conversations.find((c) => c.id === conversationId)

  return (
    <div>
      <h1 className="mb-8 font-display text-3xl">Messages</h1>

      <div className="grid grid-cols-1 gap-6 md:grid-cols-[300px_1fr]">
        <ConversationList
          conversations={conversations}
          activeId={conversationId}
          isPending={conversationsQuery.isPending}
        />

        {conversationId ? (
          <ConversationThread conversationId={conversationId} conversation={activeConversation} />
        ) : (
          <div className="flex items-center justify-center rounded-xl border-[1.5px] border-ink/15 bg-surface p-10 text-center font-ui text-sm text-brown-2">
            Sélectionnez une conversation pour afficher les messages.
          </div>
        )}
      </div>
    </div>
  )
}

function ConversationList({
  conversations,
  activeId,
  isPending,
}: {
  conversations: Conversation[]
  activeId?: string
  isPending: boolean
}) {
  return (
    <div className="h-fit rounded-xl border-[1.5px] border-ink/15 bg-surface">
      {isPending && <p className="p-4 font-ui text-sm text-brown-2">Chargement…</p>}
      {!isPending && conversations.length === 0 && (
        <p className="p-4 font-ui text-sm text-brown-2">Aucune conversation pour le moment.</p>
      )}
      <ul>
        {conversations.map((c) => (
          <li key={c.id} className="border-b border-ink/10 last:border-0">
            <Link
              to={`/messages/${c.id}`}
              className={`block px-4 py-3 font-ui text-sm ${activeId === c.id ? 'bg-paper' : ''}`}
            >
              <div className="flex items-center justify-between gap-2">
                <span className="font-semibold text-ink">{c.counterpartDisplayName}</span>
                {c.hasUnread && <span className="h-2 w-2 shrink-0 rounded-full bg-burnt" aria-label="Non lu" />}
              </div>
              <div className="truncate text-xs text-brown-2">{c.listingTitle}</div>
              {c.lastMessageBody && <div className="mt-1 truncate text-xs text-brown-3">{c.lastMessageBody}</div>}
            </Link>
          </li>
        ))}
      </ul>
    </div>
  )
}

function ConversationThread({
  conversationId,
  conversation,
}: {
  conversationId: string
  conversation?: Conversation
}) {
  const { user } = useAuth()
  const messagesQuery = useMessages(conversationId)
  const sendMessage = useSendMessage(conversationId)
  const [body, setBody] = useState('')
  const [error, setError] = useState<string | null>(null)
  const messages = messagesQuery.data ?? []

  async function handleSubmit(event: FormEvent) {
    event.preventDefault()
    setError(null)
    try {
      await sendMessage.mutateAsync(body)
      setBody('')
    } catch (err) {
      setError(err instanceof ApiError ? err.message : "Erreur lors de l'envoi du message.")
    }
  }

  return (
    <div className="flex flex-col rounded-xl border-[1.5px] border-ink/15 bg-surface">
      <div className="border-b border-ink/10 px-4 py-3">
        <div className="font-ui text-sm font-semibold text-ink">{conversation?.counterpartDisplayName}</div>
        <div className="font-ui text-xs text-brown-2">{conversation?.listingTitle}</div>
      </div>

      <div className="flex-1 space-y-3 p-4">
        {messagesQuery.isPending && <p className="font-ui text-sm text-brown-2">Chargement…</p>}
        {messagesQuery.isSuccess && messages.length === 0 && (
          <p className="font-ui text-sm text-brown-2">Aucun message pour le moment, lancez la conversation.</p>
        )}
        {messages.map((m) => (
          <div
            key={m.id}
            className={`max-w-[75%] rounded-xl px-3.5 py-2.5 font-ui text-sm ${
              m.senderId === user?.userId ? 'ml-auto bg-ink text-card' : 'bg-paper text-ink'
            }`}
          >
            {m.body}
          </div>
        ))}
      </div>

      <p className="border-t border-ink/10 px-4 py-2 font-ui text-[11px] text-brown-2">
        🔒 Le partage de coordonnées personnelles (email, téléphone) n'est pas autorisé.
      </p>

      <form onSubmit={handleSubmit} className="flex gap-2 border-t border-ink/10 p-3">
        <input
          value={body}
          onChange={(e) => setBody(e.target.value)}
          placeholder="Écrivez votre message…"
          className="flex-1 rounded-lg border-[1.5px] border-ink bg-surface px-3.5 py-2.5 font-ui text-sm text-ink"
        />
        <button
          type="submit"
          disabled={sendMessage.isPending || body.trim().length === 0}
          className="rounded-lg bg-burnt px-4 py-2.5 font-ui text-sm font-semibold text-surface disabled:opacity-50"
        >
          Envoyer
        </button>
      </form>
      {error && <p className="px-4 pb-3 font-ui text-xs font-medium text-burnt">{error}</p>}
    </div>
  )
}
