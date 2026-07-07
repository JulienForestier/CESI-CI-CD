import { screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import { NotificationsPage } from './NotificationsPage'
import * as notificationsApi from '../api/notifications'
import * as AuthContext from '../context/AuthContext'
import { renderWithProviders } from '../test/renderWithProviders'
import type { AppNotification } from '../types'

vi.mock('../api/notifications')
vi.mock('../context/AuthContext', async () => {
  const actual = await vi.importActual<typeof AuthContext>('../context/AuthContext')
  return { ...actual, useAuth: vi.fn() }
})

const unreadNotification: AppNotification = {
  id: 'notif-1',
  title: "Nouvelle annonce dans vos centres d'intérêt",
  message: '« Figurine rare » vient d\'être publiée dans la catégorie Figurines.',
  type: 'NewListingMatch',
  isRead: false,
  createdAt: new Date().toISOString(),
  listingId: 'listing-1',
}

const readNotification: AppNotification = {
  id: 'notif-2',
  title: 'Votre annonce a été validée',
  message: '« Figurine rare » est maintenant publiée sur Collector.shop.',
  type: 'ListingApproved',
  isRead: true,
  createdAt: new Date().toISOString(),
  listingId: 'listing-1',
}

describe('NotificationsPage', () => {
  beforeEach(() => {
    vi.restoreAllMocks()
    vi.mocked(AuthContext.useAuth).mockReturnValue({
      user: { token: 'jwt-token', userId: 'user-1', email: 'a@b.com', displayName: 'A', isAdmin: false },
      login: vi.fn(),
      register: vi.fn(),
      logout: vi.fn(),
    })
  })

  it('shows an empty state when there are no notifications', async () => {
    vi.mocked(notificationsApi.getNotifications).mockResolvedValue([])

    renderWithProviders(<NotificationsPage />)

    expect(await screen.findByText("Vous n'avez aucune notification pour le moment.")).toBeInTheDocument()
  })

  it('shows an error message when loading fails', async () => {
    vi.mocked(notificationsApi.getNotifications).mockRejectedValue(new Error('boom'))

    renderWithProviders(<NotificationsPage />)

    expect(await screen.findByText('Impossible de charger vos notifications.')).toBeInTheDocument()
  })

  it('shows notification cards with title and message', async () => {
    vi.mocked(notificationsApi.getNotifications).mockResolvedValue([unreadNotification, readNotification])

    renderWithProviders(<NotificationsPage />)

    expect(await screen.findByText("Nouvelle annonce dans vos centres d'intérêt")).toBeInTheDocument()
    expect(screen.getByText('Votre annonce a été validée')).toBeInTheDocument()
  })

  it('disables "Tout marquer comme lu" when there is nothing unread', async () => {
    vi.mocked(notificationsApi.getNotifications).mockResolvedValue([readNotification])

    renderWithProviders(<NotificationsPage />)

    expect(await screen.findByRole('button', { name: 'Tout marquer comme lu' })).toBeDisabled()
  })

  it('marks all notifications as read when clicking the button', async () => {
    vi.mocked(notificationsApi.getNotifications).mockResolvedValue([unreadNotification])
    vi.mocked(notificationsApi.markAllNotificationsRead).mockResolvedValue(undefined)

    renderWithProviders(<NotificationsPage />)

    await screen.findByText(unreadNotification.title)
    const button = screen.getByRole('button', { name: 'Tout marquer comme lu' })
    expect(button).toBeEnabled()
    await userEvent.click(button)

    expect(notificationsApi.markAllNotificationsRead).toHaveBeenCalledWith('jwt-token')
  })
})
