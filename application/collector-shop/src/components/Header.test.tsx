import { render, screen } from '@testing-library/react'
import { QueryClientProvider } from '@tanstack/react-query'
import userEvent from '@testing-library/user-event'
import { MemoryRouter } from 'react-router-dom'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import { Header } from './Header'
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

function renderHeader() {
  return render(
    <QueryClientProvider client={createTestQueryClient()}>
      <MemoryRouter initialEntries={['/']}>
        <Header />
      </MemoryRouter>
    </QueryClientProvider>,
  )
}

describe('Header', () => {
  beforeEach(() => {
    vi.mocked(chatApi.getConversations).mockResolvedValue([])
    vi.mocked(notificationsApi.getNotifications).mockResolvedValue([])
  })

  it('shows login/register links when logged out', () => {
    vi.mocked(AuthContext.useAuth).mockReturnValue({
      isLoading: false,
      user: null,
      login: vi.fn(),
      register: vi.fn(),
      logout: vi.fn(),
      updateDisplayName: vi.fn(),
    })

    renderHeader()

    expect(screen.getByText('Connexion')).toBeInTheDocument()
    expect(screen.getByText('Vendre un objet')).toBeInTheDocument()
  })

  it('shows a link to the profile page when logged in', () => {
    vi.mocked(AuthContext.useAuth).mockReturnValue({
      isLoading: false,
      user: { userId: 'u', email: 'a@b.com', displayName: 'Alice', isAdmin: false },
      login: vi.fn(),
      register: vi.fn(),
      logout: vi.fn(),
      updateDisplayName: vi.fn(),
    })

    renderHeader()

    expect(screen.getByTitle('Alice')).toHaveAttribute('href', '/profil')
    expect(screen.getByText('Mes annonces')).toBeInTheDocument()
    expect(screen.queryByRole('link', { name: 'Modération' })).not.toBeInTheDocument()
    expect(screen.queryByRole('button', { name: 'Se déconnecter' })).not.toBeInTheDocument()
  })

  it('shows the moderation link for admin users', () => {
    vi.mocked(AuthContext.useAuth).mockReturnValue({
      isLoading: false,
      user: { userId: 'u', email: 'admin@collector.shop', displayName: 'Admin', isAdmin: true },
      login: vi.fn(),
      register: vi.fn(),
      logout: vi.fn(),
      updateDisplayName: vi.fn(),
    })

    renderHeader()

    expect(screen.getByRole('link', { name: 'Modération' })).toBeInTheDocument()
  })

  it('shows an unread badge when conversations have unread messages', async () => {
    vi.mocked(chatApi.getConversations).mockResolvedValue([
      {
        id: 'conv-1',
        listingId: 'listing-1',
        listingTitle: 'Figurine',
        counterpartId: 'seller-1',
        counterpartDisplayName: 'Vendeur',
        lastMessageBody: 'Bonjour',
        lastMessageAt: new Date().toISOString(),
        hasUnread: true,
      },
    ])
    vi.mocked(AuthContext.useAuth).mockReturnValue({
      isLoading: false,
      user: { userId: 'u', email: 'a@b.com', displayName: 'Alice', isAdmin: false },
      login: vi.fn(),
      register: vi.fn(),
      logout: vi.fn(),
      updateDisplayName: vi.fn(),
    })

    renderHeader()

    expect(await screen.findByText('1')).toBeInTheDocument()
  })

  it('shows an unread badge when there are unread notifications', async () => {
    vi.mocked(notificationsApi.getNotifications).mockResolvedValue([
      {
        id: 'notif-1',
        title: "Nouvelle annonce dans vos centres d'intérêt",
        message: 'Une annonce correspond à vos préférences.',
        type: 'NewListingMatch',
        isRead: false,
        createdAt: new Date().toISOString(),
        listingId: 'listing-1',
      },
    ])
    vi.mocked(AuthContext.useAuth).mockReturnValue({
      isLoading: false,
      user: { userId: 'u', email: 'a@b.com', displayName: 'Alice', isAdmin: false },
      login: vi.fn(),
      register: vi.fn(),
      logout: vi.fn(),
      updateDisplayName: vi.fn(),
    })

    renderHeader()

    expect(await screen.findByRole('link', { name: 'Notifications' })).toBeInTheDocument()
    expect(await screen.findByText('1')).toBeInTheDocument()
  })

  it('shows the centres-interet link for logged-in users', () => {
    vi.mocked(AuthContext.useAuth).mockReturnValue({
      isLoading: false,
      user: { userId: 'u', email: 'a@b.com', displayName: 'Alice', isAdmin: false },
      login: vi.fn(),
      register: vi.fn(),
      logout: vi.fn(),
      updateDisplayName: vi.fn(),
    })

    renderHeader()

    expect(screen.getByRole('link', { name: "Centres d'intérêt" })).toBeInTheDocument()
  })

  it('toggles the mobile menu when the burger button is clicked', async () => {
    vi.mocked(AuthContext.useAuth).mockReturnValue({
      isLoading: false,
      user: null,
      login: vi.fn(),
      register: vi.fn(),
      logout: vi.fn(),
      updateDisplayName: vi.fn(),
    })

    renderHeader()

    const burger = screen.getByRole('button', { name: 'Ouvrir le menu' })
    expect(burger).toHaveAttribute('aria-expanded', 'false')
    expect(screen.getAllByRole('link', { name: 'Catalogue' })).toHaveLength(1)

    await userEvent.click(burger)

    const closeButton = screen.getByRole('button', { name: 'Fermer le menu' })
    expect(closeButton).toHaveAttribute('aria-expanded', 'true')
    expect(screen.getAllByRole('link', { name: 'Catalogue' })).toHaveLength(2)

    await userEvent.click(closeButton)

    expect(screen.getByRole('button', { name: 'Ouvrir le menu' })).toHaveAttribute('aria-expanded', 'false')
    expect(screen.getAllByRole('link', { name: 'Catalogue' })).toHaveLength(1)
  })

  it('shows the profile link and utility links in the mobile menu', async () => {
    vi.mocked(AuthContext.useAuth).mockReturnValue({
      isLoading: false,
      user: { userId: 'u', email: 'admin@collector.shop', displayName: 'Admin', isAdmin: true },
      login: vi.fn(),
      register: vi.fn(),
      logout: vi.fn(),
      updateDisplayName: vi.fn(),
    })

    renderHeader()

    expect(screen.getByRole('link', { name: 'Modération' })).toBeInTheDocument()
    expect(screen.getByTitle('Admin')).toBeInTheDocument()

    await userEvent.click(screen.getByRole('button', { name: 'Ouvrir le menu' }))

    expect(screen.getAllByRole('link', { name: /Modération/ })).toHaveLength(2)
    const profileLinks = screen.getAllByRole('link', { name: /Mon profil/ })
    expect(profileLinks).toHaveLength(2)
    expect(screen.getByText('Mon profil')).toBeInTheDocument()

    await userEvent.click(profileLinks[1])

    expect(screen.getByRole('button', { name: 'Ouvrir le menu' })).toHaveAttribute('aria-expanded', 'false')
  })

  it('closes the mobile menu when a link inside it is clicked', async () => {
    vi.mocked(AuthContext.useAuth).mockReturnValue({
      isLoading: false,
      user: null,
      login: vi.fn(),
      register: vi.fn(),
      logout: vi.fn(),
      updateDisplayName: vi.fn(),
    })

    renderHeader()

    await userEvent.click(screen.getByRole('button', { name: 'Ouvrir le menu' }))
    const catalogueLinks = screen.getAllByRole('link', { name: 'Catalogue' })
    await userEvent.click(catalogueLinks[1])

    expect(screen.getByRole('button', { name: 'Ouvrir le menu' })).toHaveAttribute('aria-expanded', 'false')
  })
})
