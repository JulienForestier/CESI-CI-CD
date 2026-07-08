import { render, screen } from '@testing-library/react'
import { QueryClientProvider } from '@tanstack/react-query'
import { MemoryRouter, Route, Routes } from 'react-router-dom'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import { Layout } from './Layout'
import * as chatApi from '../api/chat'
import * as notificationsApi from '../api/notifications'
import * as AuthContext from '../context/AuthContext'
import { createTestQueryClient } from '../test/queryClient'

vi.mock('../api/chat')
vi.mock('../api/notifications')
vi.mock('../context/AuthContext', async () => {
  const actual = await vi.importActual<typeof AuthContext>('../context/AuthContext')
  return { ...actual, useAuth: vi.fn() }
})

describe('Layout', () => {
  beforeEach(() => {
    vi.mocked(chatApi.getConversations).mockResolvedValue([])
    vi.mocked(notificationsApi.getNotifications).mockResolvedValue([])
    vi.mocked(AuthContext.useAuth).mockReturnValue({
      user: null,
      login: vi.fn(),
      register: vi.fn(),
      logout: vi.fn(),
      updateDisplayName: vi.fn(),
    })
  })

  it('renders the header, the routed page content, and the footer', () => {
    render(
      <QueryClientProvider client={createTestQueryClient()}>
        <MemoryRouter initialEntries={['/']}>
          <Routes>
            <Route element={<Layout />}>
              <Route index element={<div>Accueil</div>} />
            </Route>
          </Routes>
        </MemoryRouter>
      </QueryClientProvider>,
    )

    expect(screen.getByRole('link', { name: /Collector\.shop/ })).toBeInTheDocument()
    expect(screen.getByText('Accueil')).toBeInTheDocument()
    expect(screen.getByText('Paiement sécurisé')).toBeInTheDocument()
  })
})
