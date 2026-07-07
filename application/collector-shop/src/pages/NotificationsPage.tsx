import { useMarkAllNotificationsRead, useNotifications } from '../hooks/useNotifications'
import type { AppNotification, NotificationType } from '../types'

const TYPE_ICONS: Record<NotificationType, string> = {
  NewListingMatch: '🆕',
  ListingApproved: '✅',
  ListingRejected: '⚠️',
}

const TYPE_BORDER_CLASSES: Record<NotificationType, string> = {
  NewListingMatch: 'border-l-burnt',
  ListingApproved: 'border-l-teal',
  ListingRejected: 'border-l-burnt',
}

const dateFormatter = new Intl.DateTimeFormat('fr-FR', { dateStyle: 'medium', timeStyle: 'short' })

export function NotificationsPage() {
  const notificationsQuery = useNotifications()
  const markAllRead = useMarkAllNotificationsRead()
  const notifications = notificationsQuery.data ?? []
  const hasUnread = notifications.some((n) => !n.isRead)

  return (
    <div>
      <div className="mb-8 flex items-center justify-between">
        <h1 className="font-display text-3xl">Notifications</h1>
        <button
          type="button"
          onClick={() => markAllRead.mutate()}
          disabled={!hasUnread || markAllRead.isPending}
          className="rounded-full border-[1.5px] border-ink px-4 py-2 font-ui text-xs font-semibold text-ink disabled:opacity-50"
        >
          Tout marquer comme lu
        </button>
      </div>

      {notificationsQuery.isPending && <p className="font-ui text-brown-2">Chargement…</p>}
      {notificationsQuery.isError && (
        <p className="font-ui text-burnt">Impossible de charger vos notifications.</p>
      )}
      {notificationsQuery.isSuccess && notifications.length === 0 && (
        <p className="font-ui text-brown-2">Vous n'avez aucune notification pour le moment.</p>
      )}

      <div className="flex flex-col gap-3">
        {notifications.map((notification) => (
          <NotificationCard key={notification.id} notification={notification} />
        ))}
      </div>
    </div>
  )
}

function NotificationCard({ notification }: { notification: AppNotification }) {
  return (
    <div
      className={`rounded-xl border-[1.5px] border-ink/15 bg-surface p-4 border-l-4 ${TYPE_BORDER_CLASSES[notification.type]} ${
        notification.isRead ? 'opacity-60' : ''
      }`}
    >
      <div className="flex items-start gap-3">
        <span className="text-xl">{TYPE_ICONS[notification.type]}</span>
        <div className="flex-1">
          <div className="font-ui text-sm font-bold text-ink">{notification.title}</div>
          <p className="mt-1 font-ui text-sm text-brown-1">{notification.message}</p>
          <div className="mt-2 font-ui text-[11px] text-brown-2">
            {dateFormatter.format(new Date(notification.createdAt))}
          </div>
        </div>
      </div>
    </div>
  )
}
